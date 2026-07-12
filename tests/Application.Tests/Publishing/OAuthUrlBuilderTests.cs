using Domain.Enums;

namespace Application.Tests.Publishing;

public class OAuthUrlBuilderTests
{
    [Fact]
    public void Given_YouTube_When_Build_Then_UsesGoogleConsentEndpointWithUploadScope()
    {
        var url = OAuthUrlBuilder.YouTube("client-123", "https://app.local/cb", "state-xyz");

        url.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth?");
        url.Should().Contain("client_id=client-123");
        url.Should().Contain("scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fyoutube.upload");
        url.Should().Contain("access_type=offline");
        url.Should().Contain("state=state-xyz");
    }

    [Fact]
    public void Given_RedirectUri_When_Build_Then_UriIsPercentEncoded()
    {
        var url = OAuthUrlBuilder.Facebook("app", "https://app.local/callback?x=1", "s");

        url.Should().StartWith("https://www.facebook.com/v19.0/dialog/oauth?");
        url.Should().Contain("redirect_uri=https%3A%2F%2Fapp.local%2Fcallback%3Fx%3D1");
    }

    [Fact]
    public void Given_TikTok_When_Build_Then_UsesClientKeyParameter()
    {
        var url = OAuthUrlBuilder.TikTok("tt-key", "https://app.local/cb", "s");

        url.Should().StartWith("https://www.tiktok.com/v2/auth/authorize/?");
        url.Should().Contain("client_key=tt-key");
        url.Should().NotContain("client_id=");
    }

    [Theory]
    [InlineData(PublishPlatform.YouTube, "accounts.google.com")]
    [InlineData(PublishPlatform.Facebook, "facebook.com")]
    [InlineData(PublishPlatform.TikTok, "tiktok.com")]
    public void Given_Platform_When_BuildDispatch_Then_RoutesToCorrectProvider(PublishPlatform platform, string expectedHost)
    {
        var url = OAuthUrlBuilder.Build(platform, "id", "https://app.local/cb", "s");

        url.Should().Contain(expectedHost);
    }
}
