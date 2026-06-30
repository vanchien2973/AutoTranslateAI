using Infrastructure.Media.Downloader;

namespace Infrastructure.Tests.Media;

public class YtDlpArgumentsTests
{
    [Fact]
    public void Given_UrlAndOutputDir_When_BuildDownload_Then_IncludesOutputTemplateAndFinalPathPrint()
    {
        // Arrange
        const string url = "https://youtu.be/abc";
        const string outputDir = "/work/job1";

        // Act
        var args = YtDlpArguments.BuildDownload(url, outputDir);

        // Assert
        args.Should().ContainInOrder("-o", Path.Combine(outputDir, "source.%(ext)s"));
        args.Should().ContainInOrder("--print", "after_move:filepath");
        args.Should().Contain(url);
        args.Should().Contain("--no-playlist");
        args.Should().Contain("--no-simulate");
    }

    [Fact]
    public void Given_AnyUrl_When_BuildDownload_Then_PutsUrlFirst()
    {
        // Arrange
        const string url = "URL";

        // Act
        var args = YtDlpArguments.BuildDownload(url, "/out");

        // Assert
        args[0].Should().Be(url);
    }
}
