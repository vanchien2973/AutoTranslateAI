using Infrastructure.Media.Downloader;

namespace Infrastructure.Tests.Media;

public class YtDlpOutputParserTests
{
    [Fact]
    public void Given_StdoutWithDestinationAndPath_When_ParseFinalPath_Then_ReturnsLastNonEmptyLine()
    {
        // Arrange
        const string stdout = "[download] Destination: x\n/work/job1/source.mp4\n";

        // Act
        var path = YtDlpOutputParser.ParseFinalPath(stdout);

        // Assert
        path.Should().Be("/work/job1/source.mp4");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_EmptyOrBlankStdout_When_ParseFinalPath_Then_ReturnsNull(string stdout)
    {
        // Act
        var path = YtDlpOutputParser.ParseFinalPath(stdout);

        // Assert
        path.Should().BeNull();
    }
}
