using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Tests;

public class DubbingJobSubtitleStyleTests
{
    private static DubbingJob NewJob() =>
        new(sourceUrl: "https://example.com/v", localFilePath: null, sourceLanguage: null,
            audioLanguage: "vi", subtitleLanguage: "vi", enableDubbing: true);

    [Fact]
    public void Given_NoStyleSet_When_Created_Then_DefaultsApply()
    {
        var job = NewJob();

        job.SubtitleFontFamily.Should().BeNull();
        job.SubtitleFontSize.Should().Be(24);
        job.SubtitlePosition.Should().Be(SubtitlePosition.Bottom);
        job.SubtitleBold.Should().BeFalse();
        job.SubtitleItalic.Should().BeFalse();
    }

    [Fact]
    public void Given_ValidValues_When_SetSubtitleStyle_Then_StoresThem()
    {
        var job = NewJob();

        job.SetSubtitleStyle("Noto Sans", 32, SubtitlePosition.Top, bold: true, italic: false);

        job.SubtitleFontFamily.Should().Be("Noto Sans");
        job.SubtitleFontSize.Should().Be(32);
        job.SubtitlePosition.Should().Be(SubtitlePosition.Top);
        job.SubtitleBold.Should().BeTrue();
        job.SubtitleItalic.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_BlankFontFamily_When_SetSubtitleStyle_Then_StoresNull(string family)
    {
        var job = NewJob();

        job.SetSubtitleStyle(family, 24, SubtitlePosition.Bottom, bold: false, italic: false);

        job.SubtitleFontFamily.Should().BeNull();
    }

    [Theory]
    [InlineData(DubbingJob.MinSubtitleFontSize - 1)]
    [InlineData(DubbingJob.MaxSubtitleFontSize + 1)]
    [InlineData(0)]
    public void Given_FontSizeOutOfRange_When_SetSubtitleStyle_Then_Throws(int fontSize)
    {
        var act = () => NewJob().SetSubtitleStyle(null, fontSize, SubtitlePosition.Bottom, false, false);

        act.Should().Throw<BusinessRuleViolationException>();
    }
}
