using Infrastructure.Storage;

namespace Infrastructure.Tests.Storage;

public class R2UrlResolverTests
{
    [Fact]
    public void Given_PublicUrlAndKey_When_Resolve_Then_JoinsWithoutDoubleSlash()
    {
        // Act
        var url = R2UrlResolver.Resolve("https://cdn.example.com/", "/videos/out.mp4");

        // Assert
        url.Should().Be("https://cdn.example.com/videos/out.mp4");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Given_NoPublicUrl_When_Resolve_Then_ReturnsKey(string? publicUrl)
    {
        // Act
        var url = R2UrlResolver.Resolve(publicUrl, "videos/out.mp4");

        // Assert
        url.Should().Be("videos/out.mp4");
    }
}
