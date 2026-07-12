using System.Text.Json;
using Application.Dtos;
using Application.Helpers;
using Application.Interfaces;
using Domain.Enums;

namespace Infrastructure.Publishing;

// Facebook Login: Exchange the code for a user token, then get the Page + Page access token (the token is used to upload videos to the Page).
public sealed class FacebookOAuthProvider : IOAuthProvider
{
    private const string GraphVersion = "v19.0";
    private const string TokenEndpoint = $"https://graph.facebook.com/{GraphVersion}/oauth/access_token";
    private const string AccountsEndpoint = $"https://graph.facebook.com/{GraphVersion}/me/accounts";

    private readonly IHttpClientFactory _httpClientFactory;

    public FacebookOAuthProvider(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public PublishPlatform Platform => PublishPlatform.Facebook;

    public string BuildAuthorizationUrl(OAuthAppCredentials app, string redirectUri, string state) =>
        OAuthUrlBuilder.Facebook(app.ClientId, redirectUri, state);

    public async Task<ChannelTokens> ExchangeCodeAsync(
        OAuthAppCredentials app,
        string code,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var http = _httpClientFactory.CreateClient(nameof(FacebookOAuthProvider));

        var tokenUrl = $"{TokenEndpoint}?client_id={Uri.EscapeDataString(app.ClientId)}"
            + $"&client_secret={Uri.EscapeDataString(app.ClientSecret)}"
            + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
            + $"&code={Uri.EscapeDataString(code)}";

        using var tokenResponse = await http.GetAsync(tokenUrl, cancellationToken);
        await PublishHttp.EnsureSuccessAsync(tokenResponse, "Facebook token exchange", cancellationToken);

        using var token = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken));
        var userToken = token.RootElement.GetProperty("access_token").GetString()!;
        var expiresAt = token.RootElement.TryGetProperty("expires_in", out var expires)
            ? DateTimeOffset.UtcNow.AddSeconds(expires.GetInt32())
            : (DateTimeOffset?)null;

        // Đăng video cần Page access token, không phải user token → lấy Page đầu tiên user quản lý.
        using var pagesResponse = await http.GetAsync(
            $"{AccountsEndpoint}?access_token={Uri.EscapeDataString(userToken)}", cancellationToken);
        await PublishHttp.EnsureSuccessAsync(pagesResponse, "Facebook page lookup", cancellationToken);

        using var pages = JsonDocument.Parse(await pagesResponse.Content.ReadAsStringAsync(cancellationToken));
        var data = pages.RootElement.GetProperty("data");
        if (data.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("The Facebook account does not manage any Page to publish to.");
        }

        var page = data[0];
        var pageId = page.GetProperty("id").GetString()!;
        var pageName = page.GetProperty("name").GetString() ?? pageId;
        var pageToken = page.GetProperty("access_token").GetString()!;

        return new ChannelTokens(pageId, pageName, pageToken, null, expiresAt);
    }
}
