using Infrastructure.AI.SpeechToText;

namespace Infrastructure.Tests.AI.SpeechToText;

public class WhisperModelUrlResolverTests
{
    [Theory]
    [InlineData("base", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin")]
    [InlineData("small", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin")]
    [InlineData("large-v3", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin")]
    public void Given_KnownModel_When_BuildDownloadUrl_Then_ReturnsGgmlUrl(string model, string expectedUrl)
    {
        // Act
        var url = WhisperModelUrlResolver.BuildDownloadUrl(model);

        // Assert
        url.Should().Be(expectedUrl);
    }

    [Fact]
    public void Given_ModelWithDifferentCase_When_BuildDownloadUrl_Then_NormalizesToLowercase()
    {
        // Act
        var url = WhisperModelUrlResolver.BuildDownloadUrl("BASE");

        // Assert
        url.Should().EndWith("ggml-base.bin");
    }

    [Theory]
    [InlineData("")]
    [InlineData("bogus")]
    public void Given_UnknownModel_When_BuildDownloadUrl_Then_Throws(string model)
    {
        // Act
        var act = () => WhisperModelUrlResolver.BuildDownloadUrl(model);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
