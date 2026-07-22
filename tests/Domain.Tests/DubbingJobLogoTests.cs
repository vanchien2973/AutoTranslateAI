using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Tests;

public class DubbingJobLogoTests
{
    private static DubbingJob NewJob() =>
        new(sourceUrl: "https://example.com/v", localFilePath: null, sourceLanguage: null,
            audioLanguage: "vi", subtitleLanguage: "vi", enableDubbing: true);

    [Fact]
    public void Given_NoLogoSet_When_Created_Then_KeyIsNullAndDefaultsApply()
    {
        var job = NewJob();

        job.LogoStorageKey.Should().BeNull();
        job.LogoPosition.Should().Be(LogoPosition.BottomRight);
        job.LogoScalePercent.Should().Be(0.1);
        job.LogoMargin.Should().Be(16);
    }

    [Fact]
    public void Given_ValidValues_When_SetLogo_Then_StoresThem()
    {
        var job = NewJob();

        job.SetLogo("logos/abc.png", LogoPosition.TopLeft, 0.25, 24);

        job.LogoStorageKey.Should().Be("logos/abc.png");
        job.LogoPosition.Should().Be(LogoPosition.TopLeft);
        job.LogoScalePercent.Should().Be(0.25);
        job.LogoMargin.Should().Be(24);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_BlankKey_When_SetLogo_Then_Throws(string key)
    {
        var act = () => NewJob().SetLogo(key, LogoPosition.TopLeft, 0.1, 16);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    [InlineData(1.5)]
    public void Given_ScaleOutsideFrame_When_SetLogo_Then_Throws(double scale)
    {
        var act = () => NewJob().SetLogo("logos/a.png", LogoPosition.TopLeft, scale, 16);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Given_NegativeMargin_When_SetLogo_Then_Throws()
    {
        var act = () => NewJob().SetLogo("logos/a.png", LogoPosition.TopLeft, 0.1, -1);

        act.Should().Throw<BusinessRuleViolationException>();
    }
}
