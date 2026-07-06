namespace Application.Tests.Pipeline;

public class RateFactorFitTests
{
    [Fact]
    public void Given_NaturalFitsWindow_When_Fit_Then_NoExtension()
    {
        // Arrange — 3s slot, 2.5s of speech (fits; would be slowed, not sped up).
        // Act
        var timing = RateFactorCalculator.Fit(start: 0, end: 3, naturalDurationSeconds: 2.5, nextStart: 10);

        // Assert
        timing.End.Should().Be(3);
        timing.RateFactor.Should().BeApproximately(2.5 / 3.0, 1e-9);
    }

    [Fact]
    public void Given_OverflowAndGapCoversIt_When_Fit_Then_ExtendsToNaturalAndFactorOne()
    {
        // Arrange — 3s slot, 4s speech, next segment starts at 6 (3s of silence after the slot).
        // Act
        var timing = RateFactorCalculator.Fit(start: 0, end: 3, naturalDurationSeconds: 4, nextStart: 6);

        // Assert
        timing.End.Should().BeApproximately(4, 1e-9);
        timing.RateFactor.Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void Given_OverflowAndGapPartial_When_Fit_Then_ExtendsToNextStartAndReducesFactor()
    {
        // Arrange — 3s slot, 5s speech, only 0.5s gap before the next segment (starts at 3.5).
        // Act
        var timing = RateFactorCalculator.Fit(start: 0, end: 3, naturalDurationSeconds: 5, nextStart: 3.5);

        // Assert
        timing.End.Should().BeApproximately(3.5, 1e-9);      // capped at the next segment's start
        timing.RateFactor.Should().BeApproximately(5.0 / 3.5, 1e-9); // < the un-borrowed 5/3
    }

    [Fact]
    public void Given_NoGap_When_Fit_Then_NoExtension()
    {
        // Arrange — next segment starts exactly where this one ends.
        // Act
        var timing = RateFactorCalculator.Fit(start: 0, end: 3, naturalDurationSeconds: 4, nextStart: 3);

        // Assert
        timing.End.Should().Be(3);
        timing.RateFactor.Should().BeApproximately(4.0 / 3.0, 1e-9);
    }

    [Fact]
    public void Given_LastSegment_When_Fit_Then_ExtendsToNaturalLength()
    {
        // Arrange — no following segment (nextStart = +inf).
        // Act
        var timing = RateFactorCalculator.Fit(start: 0, end: 3, naturalDurationSeconds: 4, nextStart: double.PositiveInfinity);

        // Assert
        timing.End.Should().BeApproximately(4, 1e-9);
        timing.RateFactor.Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void Given_ExtensionStillTooShort_When_Fit_Then_FactorStaysClamped()
    {
        // Arrange — 1s slot, 5s speech, only 0.5s gap; even after borrowing, ratio exceeds the clamp.
        // Act
        var timing = RateFactorCalculator.Fit(start: 0, end: 1, naturalDurationSeconds: 5, nextStart: 1.5);

        // Assert
        timing.End.Should().BeApproximately(1.5, 1e-9);
        timing.RateFactor.Should().Be(RateFactorCalculator.MaxFactor);
    }
}
