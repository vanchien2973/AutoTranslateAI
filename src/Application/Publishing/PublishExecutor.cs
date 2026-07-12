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

    public PublishExecutor(
        IChannelConnectionRepository connections,
        IPublisherFactory publishers,
        IPublishResultRepository results)
    {
        _connections = connections;
        _publishers = publishers;
        _results = results;
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
}
