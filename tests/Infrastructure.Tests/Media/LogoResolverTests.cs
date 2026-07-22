using Application.Dtos;
using Domain.Enums;
using Infrastructure.Configuration;
using Infrastructure.Media.FFmpeg;

namespace Infrastructure.Tests.Media;

public class LogoResolverTests
{
    private const string ConfiguredLogo = "https://cdn.example.com/global.png";
    private const string JobLogo = "https://r2.example.com/logos/job.png";

    private static RenderRequest Request(string? logoPath = null) =>
        new("v.mp4", "a.wav", "o.mp4")
        {
            LogoPath = logoPath,
            LogoPosition = LogoPosition.TopLeft,
            LogoScalePercent = 0.25,
            LogoMargin = 40,
        };

    [Fact]
    public void Given_JobLogo_When_ConfigAlsoHasOne_Then_JobLogoWins()
    {
        var effective = LogoResolver.Resolve(Request(JobLogo), new LogoOptions { Path = ConfiguredLogo });

        effective.LogoPath.Should().Be(JobLogo);
        effective.LogoPosition.Should().Be(LogoPosition.TopLeft);
        effective.LogoScalePercent.Should().Be(0.25);
        effective.LogoMargin.Should().Be(40);
    }

    [Fact]
    public void Given_NoJobLogo_When_ConfigHasOne_Then_FallsBackToConfig()
    {
        var options = new LogoOptions
        {
            Path = ConfiguredLogo,
            Position = LogoPosition.TopRight,
            ScalePercent = 0.2,
            Margin = 8,
        };

        var effective = LogoResolver.Resolve(Request(), options);

        effective.LogoPath.Should().Be(ConfiguredLogo);
        effective.LogoPosition.Should().Be(LogoPosition.TopRight);
        effective.LogoScalePercent.Should().Be(0.2);
        effective.LogoMargin.Should().Be(8);
    }

    [Fact]
    public void Given_NoLogoAnywhere_When_Resolve_Then_StaysWithoutWatermark()
    {
        LogoResolver.Resolve(Request(), new LogoOptions()).LogoPath.Should().BeNull();
    }

    [Fact]
    public void Given_ConfiguredLocalFileMissing_When_Resolve_Then_SkipsWatermark()
    {
        var effective = LogoResolver.Resolve(Request(), new LogoOptions { Path = "/does/not/exist.png" });

        effective.LogoPath.Should().BeNull();
    }

    [Theory]
    [InlineData("https://cdn.example.com/a.png", true)]
    [InlineData("http://cdn.example.com/a.png", true)]
    [InlineData("/nope/missing.png", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Given_Path_When_IsUsable_Then_MatchesExpectation(string? path, bool expected)
    {
        LogoResolver.IsUsable(path).Should().Be(expected);
    }
}
