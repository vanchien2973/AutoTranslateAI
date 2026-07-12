using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Publishing.ConnectChannel;

public sealed class ConnectChannelCommandHandler : IRequestHandler<ConnectChannelCommand, ConnectChannelResponse>
{
    private readonly IPlatformCredentialRepository _credentials;
    private readonly IOAuthProviderFactory _providers;
    private readonly IChannelConnectionRepository _connections;

    public ConnectChannelCommandHandler(
        IPlatformCredentialRepository credentials,
        IOAuthProviderFactory providers,
        IChannelConnectionRepository connections)
    {
        _credentials = credentials;
        _providers = providers;
        _connections = connections;
    }

    public async Task<ConnectChannelResponse> Handle(ConnectChannelCommand request, CancellationToken cancellationToken)
    {
        var credential = await _credentials.GetAsync(request.Platform, cancellationToken);
        if (credential is null)
        {
            return ConnectChannelResponse.CredentialsMissing(request.Platform.ToString());
        }

        var app = new OAuthAppCredentials(credential.ClientId, credential.ClientSecret);

        ChannelTokens tokens;
        try
        {
            tokens = await _providers.Get(request.Platform).ExchangeCodeAsync(app, request.Code, request.RedirectUri, cancellationToken);
        }
        catch (Exception exception)
        {
            return ConnectChannelResponse.ExchangeFailed(exception.Message);
        }

        var connection = new ChannelConnection(
            request.Platform,
            tokens.ChannelId,
            tokens.ChannelName,
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresAt);

        await _connections.AddAsync(connection, cancellationToken);
        await _connections.SaveChangesAsync(cancellationToken);

        return ConnectChannelResponse.Ok(connection.ToDto(DateTimeOffset.UtcNow));
    }
}
