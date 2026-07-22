using Domain.Enums;

namespace Application.Interfaces;

public interface IOAuthProvider
{
    PublishPlatform Platform { get; }

    string BuildAuthorizationUrl(OAuthAppCredentials app, string redirectUri, string state);

    Task<ChannelTokens> ExchangeCodeAsync(OAuthAppCredentials app, string code, string redirectUri, CancellationToken cancellationToken);

    Task<ChannelTokens> RefreshAsync(OAuthAppCredentials app, string refreshToken, CancellationToken cancellationToken);
}
