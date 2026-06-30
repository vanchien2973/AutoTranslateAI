using Infrastructure.AI.TextToSpeech;

namespace Infrastructure.Tests.AI.TextToSpeech;

public class SsmlBuilderTests
{
    [Theory]
    [InlineData(1.0, "+0%")]
    [InlineData(1.15, "+15%")]
    [InlineData(0.9, "-10%")]
    public void Given_RateFactor_When_RateToPercent_Then_FormatsSignedPercent(double factor, string expected)
    {
        // Act
        var percent = SsmlBuilder.RateToPercent(factor);

        // Assert
        percent.Should().Be(expected);
    }

    [Fact]
    public void Given_ExtremeRateFactor_When_RateToPercent_Then_ClampsToSupportedRange()
    {
        // Act
        var percent = SsmlBuilder.RateToPercent(10.0);

        // Assert
        percent.Should().Be("+200%");
    }

    [Fact]
    public void Given_TextWithSpecialChars_When_Build_Then_EscapesXmlAndEmbedsVoiceAndRate()
    {
        // Act
        var ssml = SsmlBuilder.Build("Tom & Jerry", "vi-VN-HoaiMyNeural", "vi", 1.1);

        // Assert
        ssml.Should().Contain("Tom &amp; Jerry");
        ssml.Should().Contain("name=\"vi-VN-HoaiMyNeural\"");
        ssml.Should().Contain("rate=\"+10%\"");
        ssml.Should().Contain("xml:lang=\"vi\"");
    }
}
