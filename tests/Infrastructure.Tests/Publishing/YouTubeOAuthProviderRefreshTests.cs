using Application.Dtos;
using Infrastructure.Publishing;

namespace Infrastructure.Tests.Publishing;

public class YouTubeOAuthProviderRefreshTests
{
    private static readonly OAuthAppCredentials App = new("client-id", "client-secret");

    [Fact]
    public async Task Given_RefreshToken_When_Refresh_Then_UsesRefreshGrant()
    {
        var http = new StubHttpClientFactory("""{"access_token":"new-access","expires_in":3600}""");

        await new YouTubeOAuthProvider(http).RefreshAsync(App, "stored-refresh", CancellationToken.None);

        http.LastRequestUri!.ToString().Should().Be("https://oauth2.googleapis.com/token");
        http.LastRequestBody.Should().Contain("grant_type=refresh_token");
        http.LastRequestBody.Should().Contain("refresh_token=stored-refresh");
        http.LastRequestBody.Should().Contain("client_id=client-id");
    }

    [Fact]
    public async Task Given_ResponseWithoutRefreshToken_When_Refresh_Then_ReturnsNullRefresh()
    {
        var http = new StubHttpClientFactory("""{"access_token":"new-access","expires_in":3600}""");

        var tokens = await new YouTubeOAuthProvider(http).RefreshAsync(App, "stored-refresh", CancellationToken.None);

        tokens.AccessToken.Should().Be("new-access");
        tokens.RefreshToken.Should().BeNull();
        tokens.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Given_ResponseWithNewRefreshToken_When_Refresh_Then_PassesItOn()
    {
        var http = new StubHttpClientFactory(
            """{"access_token":"new-access","refresh_token":"rotated","expires_in":3600}""");

        var tokens = await new YouTubeOAuthProvider(http).RefreshAsync(App, "stored-refresh", CancellationToken.None);

        tokens.RefreshToken.Should().Be("rotated");
    }

    [Fact]
    public async Task Given_ErrorResponse_When_Refresh_Then_Throws()
    {
        var http = new StubHttpClientFactory(
            """{"error":"invalid_grant"}""", System.Net.HttpStatusCode.BadRequest);

        var act = async () =>
            await new YouTubeOAuthProvider(http).RefreshAsync(App, "revoked", CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}
