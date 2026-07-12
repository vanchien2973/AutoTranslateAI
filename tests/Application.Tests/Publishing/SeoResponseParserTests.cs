namespace Application.Tests.Publishing;

public class SeoResponseParserTests
{
    [Fact]
    public void Given_ValidJson_When_TryParse_Then_ReturnsMetadata()
    {
        var success = SeoResponseParser.TryParse(
            """{"title":"T","description":"D","tags":["a","b"]}""", out var metadata, out var error);

        success.Should().BeTrue();
        error.Should().BeNull();
        metadata!.Title.Should().Be("T");
        metadata.Description.Should().Be("D");
        metadata.Tags.Should().Equal("a", "b");
    }

    [Fact]
    public void Given_MissingTitle_When_TryParse_Then_Fails()
    {
        SeoResponseParser.TryParse("""{"description":"D"}""", out _, out var error).Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Given_InvalidJson_When_TryParse_Then_Fails()
    {
        SeoResponseParser.TryParse("not json {", out _, out _).Should().BeFalse();
    }

    [Fact]
    public void Given_TitleOnly_When_TryParse_Then_EmptyDescriptionAndTags()
    {
        SeoResponseParser.TryParse("""{"title":"T"}""", out var metadata, out _).Should().BeTrue();
        metadata!.Description.Should().BeEmpty();
        metadata.Tags.Should().BeEmpty();
    }
}
