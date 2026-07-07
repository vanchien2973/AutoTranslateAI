namespace Application.Tests.Pipeline;

// Different languages expand/contract text differently, so the same source window yields a different
// natural TTS duration per language. These pin the rate-factor sync behaviour across that range.
public class TtsRateFactorLanguageTests
{
    private const double SourceWindow = 3.0;

    [Theory]
    [InlineData("en", 3.0)]     // baseline
    [InlineData("vi", 3.3)]     // Vietnamese slightly longer
    [InlineData("de", 4.2)]     // German expands
    [InlineData("ja", 3.6)]     // Japanese
    [InlineData("zh-CN", 2.4)]  // Chinese contracts
    public void Given_LanguageTextExpansion_When_Fit_Then_RateFactorStaysWithinClamp(string language, double naturalSeconds)
    {
        // Act — no trailing gap available.
        var timing = RateFactorCalculator.Fit(0, SourceWindow, naturalSeconds, nextStart: SourceWindow);

        // Assert
        timing.RateFactor.Should().BeInRange(
            RateFactorCalculator.MinFactor, RateFactorCalculator.MaxFactor, "'{0}' must stay within the rate clamp", language);
    }

    [Theory]
    [InlineData("de", 4.2)]
    [InlineData("ja", 3.6)]
    [InlineData("vi", 3.3)]
    public void Given_OverflowingLanguageWithFollowingGap_When_Fit_Then_BorrowingRelaxesRateToOne(string language, double naturalSeconds)
    {
        // Act
        var noGap = RateFactorCalculator.Fit(0, SourceWindow, naturalSeconds, nextStart: SourceWindow);
        var withGap = RateFactorCalculator.Fit(0, SourceWindow, naturalSeconds, nextStart: SourceWindow + 10);

        // Assert — borrowing silence never speeds up more, and here fully absorbs the overflow.
        withGap.RateFactor.Should().BeLessThanOrEqualTo(noGap.RateFactor + 1e-9, "borrowing helps '{0}'", language);
        withGap.RateFactor.Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void Given_ExtremeExpansionNoRoom_When_Fit_Then_RateFactorClampedAtMax()
    {
        // A very verbose translation squeezed into a 2s slot with no following gap.
        var timing = RateFactorCalculator.Fit(0, 2, naturalDurationSeconds: 6, nextStart: 2);

        timing.RateFactor.Should().Be(RateFactorCalculator.MaxFactor);
    }
}
