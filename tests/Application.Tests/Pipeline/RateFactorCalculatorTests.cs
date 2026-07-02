using Application.Pipeline;

namespace Application.Tests.Pipeline;

public class RateFactorCalculatorTests
{
    [Fact]
    public void Given_EqualDurations_When_Compute_Then_ReturnsOne()
    {
        // Act / Assert
        RateFactorCalculator.Compute(3.0, 3.0).Should().Be(1.0);
    }

    [Fact]
    public void Given_NaturalLongerThanSlot_When_Compute_Then_SpeedsUp()
    {
        // Act
        var factor = RateFactorCalculator.Compute(4.0, 3.0);

        // Assert
        factor.Should().BeApproximately(4.0 / 3.0, 1e-9);
    }

    [Theory]
    [InlineData(10.0, 1.0, 2.0)]   // would be 10x -> clamp to max 2.0
    [InlineData(1.0, 10.0, 0.5)]   // would be 0.1x -> clamp to min 0.5
    public void Given_ExtremeRatio_When_Compute_Then_ClampsToRange(double natural, double target, double expected)
    {
        // Act / Assert
        RateFactorCalculator.Compute(natural, target).Should().Be(expected);
    }

    [Theory]
    [InlineData(0.0, 3.0)]
    [InlineData(3.0, 0.0)]
    [InlineData(-1.0, 3.0)]
    public void Given_NonPositiveDuration_When_Compute_Then_ReturnsOne(double natural, double target)
    {
        // Act / Assert
        RateFactorCalculator.Compute(natural, target).Should().Be(1.0);
    }
}
