using Application.Dtos;
using Application.Helpers;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Publishing;

public sealed class PublishExecutor : IPublishExecutor
{
    private readonly IChannelConnectionRepository _connections;
    private readonly IPublisherFactory _publishers;
    private readonly IPublishResultRepository _results;
    private readonly IPlatformCredentialRepository _credentials;
    private readonly IOAuthProviderFactory _oauth;

    public PublishExecutor(
        IChannelConnectionRepository connections,
        IPublisherFactory publishers,
        IPublishResultRepository results,
        IPlatformCredentialRepository credentials,
        IOAuthProviderFactory oauth)
    {
        _connections = connections;
        _publishers = publishers;
        _results = results;
        _credentials = credentials;
        _oauth = oauth;
    }

    public async Task<IReadOnlyList<PublishTargetResult>> ExecuteAsync(
        DubbingJob job,
        IReadOnlyList<PublishTarget> targets,
        CancellationToken cancellationToken)
    {
        var storageKey = OutputStorageKey.For(job.Id);
        var outcomes = new List<PublishTargetResult>(targets.Count);

        foreach (var target in targets)
        {
            outcomes.Add(await PublishToTargetAsync(job, target, storageKey, cancellationToken));
        }

        await _results.SaveChangesAsync(cancellationToken);
        return outcomes;
    }

    private async Task<PublishTargetResult> PublishToTargetAsync(
        DubbingJob job,
        PublishTarget target,
        string storageKey,
        CancellationToken cancellationToken)
    {
        var connection = target.ConnectionId is { } id
            ? await _connections.GetByIdAsync(id, cancellationToken)
            : await _connections.GetLatestAsync(target.Platform, cancellationToken);

        if (connection is null || connection.Platform != target.Platform)
        {
            return new PublishTargetResult(target.Platform, target.ConnectionId ?? Guid.Empty, PublishStatus.Failed,
                null, null, $"No connected {target.Platform} account. Connect one before publishing.");
        }

        // An access token that died while the job was rendering is the common case for a long job
        // (YouTube's lasts about an hour), so renew it here rather than failing the upload.
        var refreshError = await TryRefreshAsync(connection, cancellationToken);
        if (refreshError is not null)
        {
            return new PublishTargetResult(target.Platform, connection.Id, PublishStatus.Failed, null, null, refreshError);
        }

        var result = new PublishResult(job.Id, target.Platform);
        await _results.AddAsync(result, cancellationToken);
        result.MarkPublishing();

        var publishRequest = new PublishRequest(
            storageKey,
            target.Title ?? $"AutoTranslateAI {job.Id:N}",
            target.Description,
            target.Tags ?? [],
            connection.AccessToken,
            connection.ChannelId);

        try
        {
            var outcome = await _publishers.Get(target.Platform).PublishAsync(publishRequest, cancellationToken);
            result.MarkPublished(outcome.ExternalId, outcome.Url);
            return new PublishTargetResult(target.Platform, connection.Id, PublishStatus.Published,
                outcome.ExternalId, outcome.Url, null);
        }
        catch (Exception exception)
        {
            result.MarkFailed(exception.Message);
            return new PublishTargetResult(target.Platform, connection.Id, PublishStatus.Failed,
                null, null, exception.Message);
        }
    }

    /// <summary>Returns null when the connection is usable, otherwise a message for the user.</summary>
    private async Task<string?> TryRefreshAsync(ChannelConnection connection, CancellationToken cancellationToken)
    {
        if (!connection.IsExpired(DateTimeOffset.UtcNow))
        {
            return null;
        }

        if (connection.RefreshToken is null)
        {
            return $"The {connection.Platform} access token expired. Reconnect the account.";
        }

        var credential = await _credentials.GetAsync(connection.Platform, cancellationToken);
        if (credential is null)
        {
            return $"No {connection.Platform} app credentials configured; cannot renew the access token.";
        }

        try
        {
            var app = new OAuthAppCredentials(credential.ClientId, credential.ClientSecret);
            var tokens = await _oauth.Get(connection.Platform).RefreshAsync(app, connection.RefreshToken, cancellationToken);

            connection.UpdateTokens(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt);
            await _connections.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (Exception exception)
        {
            return $"Could not renew the {connection.Platform} access token ({exception.Message}). Reconnect the account.";
        }
    }
}
