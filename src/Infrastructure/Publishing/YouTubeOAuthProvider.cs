using System.Text.Json;
using Application.Interfaces;
using Domain.Enums;

namespace Infrastructure.Publishing;

// Google OAuth 2.0: change code to get token them get infor channel YouTube Data API v3.
public sealed class YouTubeOAuthProvider : IOAuthProvider
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string ChannelEndpoint = "https://www.googleapis.com/youtube/v3/channels?part=snippet&mine=true";

    private readonly IHttpClientFactory _httpClientFactory;

    public YouTubeOAuthProvider(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public PublishPlatform Platform => PublishPlatform.YouTube;

    public string BuildAuthorizationUrl(OAuthAppCredentials app, string redirectUri, string state) =>
        OAuthUrlBuilder.YouTube(app.ClientId, redirectUri, state);

    public async Task<ChannelTokens> ExchangeCodeAsync(
        OAuthAppCredentials app,
        string code,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var http = _httpClientFactory.CreateClient(nameof(YouTubeOAuthProvider));

        using var tokenResponse = await http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = app.ClientId,
            ["client_secret"] = app.ClientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
        }), cancellationToken);
        await PublishHttp.EnsureSuccessAsync(tokenResponse, "Google token exchange", cancellationToken);

        using var token = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken));
        var root = token.RootElement;
        var accessToken = root.GetProperty("access_token").GetString()!;
        var refreshToken = root.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : null;
        var expiresAt = root.TryGetProperty("expires_in", out var expires)
            ? DateTimeOffset.UtcNow.AddSeconds(expires.GetInt32())
            : (DateTimeOffset?)null;

        http.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        using var channelResponse = await http.GetAsync(ChannelEndpoint, cancellationToken);
        await PublishHttp.EnsureSuccessAsync(channelResponse, "YouTube channel lookup", cancellationToken);

        using var channel = JsonDocument.Parse(await channelResponse.Content.ReadAsStringAsync(cancellationToken));
        var item = channel.RootElement.GetProperty("items")[0];
        var channelId = item.GetProperty("id").GetString()!;
        var channelName = item.GetProperty("snippet").GetProperty("title").GetString() ?? channelId;

        return new ChannelTokens(channelId, channelName, accessToken, refreshToken, expiresAt);
    }
}
