using System.Text.Json;
using Application.Interfaces;
using Domain.Enums;

namespace Infrastructure.Publishing;

// TikTok OAuth v2: Exchange the code for the token (including open_id) and then retrieve the display_name via the User Info API.
public sealed class TikTokOAuthProvider : IOAuthProvider
{
    private const string TokenEndpoint = "https://open.tiktokapis.com/v2/oauth/token/";
    private const string UserInfoEndpoint = "https://open.tiktokapis.com/v2/user/info/?fields=open_id,display_name";

    private readonly IHttpClientFactory _httpClientFactory;

    public TikTokOAuthProvider(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public PublishPlatform Platform => PublishPlatform.TikTok;

    public string BuildAuthorizationUrl(OAuthAppCredentials app, string redirectUri, string state) =>
        OAuthUrlBuilder.TikTok(app.ClientId, redirectUri, state);

    public async Task<ChannelTokens> ExchangeCodeAsync(
        OAuthAppCredentials app,
        string code,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var http = _httpClientFactory.CreateClient(nameof(TikTokOAuthProvider));

        using var tokenResponse = await http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_key"] = app.ClientId,
            ["client_secret"] = app.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri,
        }), cancellationToken);
        await PublishHttp.EnsureSuccessAsync(tokenResponse, "TikTok token exchange", cancellationToken);

        using var token = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken));
        var root = token.RootElement;
        var accessToken = root.GetProperty("access_token").GetString()!;
        var openId = root.GetProperty("open_id").GetString()!;
        var refreshToken = root.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : null;
        var expiresAt = root.TryGetProperty("expires_in", out var expires)
            ? DateTimeOffset.UtcNow.AddSeconds(expires.GetInt32())
            : (DateTimeOffset?)null;

        var displayName = await TryGetDisplayNameAsync(http, accessToken, cancellationToken) ?? openId;
        return new ChannelTokens(openId, displayName, accessToken, refreshToken, expiresAt);
    }

    public async Task<ChannelTokens> RefreshAsync(
        OAuthAppCredentials app,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var http = _httpClientFactory.CreateClient(nameof(TikTokOAuthProvider));

        using var response = await http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_key"] = app.ClientId,
            ["client_secret"] = app.ClientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
        }), cancellationToken);
        await PublishHttp.EnsureSuccessAsync(response, "TikTok token refresh", cancellationToken);

        using var token = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = token.RootElement;

        return new ChannelTokens(
            ChannelId: string.Empty,
            ChannelName: string.Empty,
            AccessToken: root.GetProperty("access_token").GetString()!,
            RefreshToken: root.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : null,
            ExpiresAt: root.TryGetProperty("expires_in", out var expires)
                ? DateTimeOffset.UtcNow.AddSeconds(expires.GetInt32())
                : null);
    }

    // Use the display name best-effort — if the scope user.info.basic is missing, skip it to avoid breaking the connection.
    private static async Task<string?> TryGetDisplayNameAsync(HttpClient http, string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
            request.Headers.Authorization = new("Bearer", accessToken);
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return json.RootElement.TryGetProperty("data", out var data)
                && data.TryGetProperty("user", out var user)
                && user.TryGetProperty("display_name", out var name)
                    ? name.GetString()
                    : null;
        }
        catch
        {
            return null;
        }
    }
}
