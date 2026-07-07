namespace Application.Tests.Translation;

public class TranslationPlannerTests
{
    [Theory]
    [InlineData("en", "en", "en", false, 0)] // audio = source, sub = source -> 0
    [InlineData("en", "vi", "vi", true, 1)]  // audio != source, sub = audio -> 1
    [InlineData("en", "vi", "en", true, 1)]  // audio != source, sub = source -> 1
    [InlineData("en", "en", "vi", false, 1)] // audio = source, sub != source -> 1
    [InlineData("en", "vi", "ja", true, 2)]  // all different -> 2
    public void Given_LanguageCombo_When_Plan_Then_ReturnsExpectedCallCount(
        string src, string audio, string sub, bool dubbing, int expected)
    {
        // Act
        var plan = TranslationPlanner.Plan(src, audio, sub, dubbing);

        // Assert
        plan.TranslationCalls.Should().Be(expected);
    }

    [Fact]
    public void Given_AudioEqualsSource_When_Plan_Then_DoesNotTranslateAudio()
    {
        TranslationPlanner.Plan("en", "en", "en", enableDubbing: true).TranslateAudio.Should().BeFalse();
    }

    [Fact]
    public void Given_AudioDiffersFromSource_When_Plan_Then_TranslatesAudio()
    {
        TranslationPlanner.Plan("en", "vi", "vi", enableDubbing: true).TranslateAudio.Should().BeTrue();
    }

    [Fact]
    public void Given_DubbingDisabled_When_Plan_Then_NeverTranslatesAudio()
    {
        var plan = TranslationPlanner.Plan("en", "vi", "vi", enableDubbing: false);
        plan.TranslateAudio.Should().BeFalse();
        // Audio stays original, so sub "vi" can't reuse a (non-existent) audio translation.
        plan.Subtitle.Should().Be(SubtitleSource.Translate);
    }

    [Fact]
    public void Given_SubtitleEqualsSource_When_Plan_Then_SubtitleIsOriginal()
    {
        TranslationPlanner.Plan("en", "vi", "en", enableDubbing: true).Subtitle.Should().Be(SubtitleSource.Original);
    }

    [Fact]
    public void Given_SubtitleEqualsTranslatedAudio_When_Plan_Then_SubtitleReusesAudio()
    {
        var plan = TranslationPlanner.Plan("en", "vi", "vi", enableDubbing: true);
        plan.Subtitle.Should().Be(SubtitleSource.ReuseAudio);
        plan.TranslationCalls.Should().Be(1);
    }

    [Fact]
    public void Given_AllLanguagesDiffer_When_Plan_Then_TranslatesBoth()
    {
        var plan = TranslationPlanner.Plan("en", "vi", "ja", enableDubbing: true);
        plan.TranslateAudio.Should().BeTrue();
        plan.Subtitle.Should().Be(SubtitleSource.Translate);
    }

    [Fact]
    public void Given_NoSubtitleLanguage_When_Plan_Then_SubtitleIsNone()
    {
        TranslationPlanner.Plan("en", "vi", null, enableDubbing: true).Subtitle.Should().Be(SubtitleSource.None);
        TranslationPlanner.Plan("en", "vi", "  ", enableDubbing: true).Subtitle.Should().Be(SubtitleSource.None);
    }

    [Fact]
    public void Given_CaseInsensitiveCodes_When_Plan_Then_TreatedAsSameLanguage()
    {
        var plan = TranslationPlanner.Plan("EN", "en", "En", enableDubbing: true);
        plan.TranslateAudio.Should().BeFalse();
        plan.Subtitle.Should().Be(SubtitleSource.Original);
        plan.TranslationCalls.Should().Be(0);
    }

    [Fact]
    public void Given_UnknownSourceLanguage_When_Plan_Then_TranslatesToBeSafe()
    {
        // Source not detected: nothing "equals" it, so audio + distinct sub are both translated.
        var plan = TranslationPlanner.Plan(null, "vi", "ja", enableDubbing: true);
        plan.TranslationCalls.Should().Be(2);
    }
}
