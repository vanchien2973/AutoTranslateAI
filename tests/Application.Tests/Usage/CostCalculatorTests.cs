using Domain.Enums;

namespace Application.Tests.Usage;

public class CostCalculatorTests
{
    private static readonly UsagePricing Pricing = new(
        LlmInputPerMillionTokens: 0.10m,
        LlmOutputPerMillionTokens: 0.40m,
        TtsPerMillionCharacters: 16.0m,
        SttPerMinute: 0.006m);

    [Fact]
    public void Given_Tokens_When_Estimate_Then_PricesInputAndOutputSeparately()
    {
        // 2M input @ $0.10/M + 1M output @ $0.40/M = 0.20 + 0.40
        var cost = CostCalculator.Estimate(UsageUnit.Tokens, 2_000_000, 1_000_000, Pricing);

        cost.Should().Be(0.60m);
    }

    [Fact]
    public void Given_Characters_When_Estimate_Then_UsesTtsRate()
    {
        CostCalculator.Estimate(UsageUnit.Characters, 500_000, 0, Pricing).Should().Be(8.0m);
    }

    [Fact]
    public void Given_Seconds_When_Estimate_Then_ConvertsToMinutes()
    {
        // 120 seconds = 2 minutes @ $0.006 = 0.012
        CostCalculator.Estimate(UsageUnit.Seconds, 120, 0, Pricing).Should().Be(0.012m);
    }

    [Fact]
    public void Given_ZeroUnits_When_Estimate_Then_Zero()
    {
        CostCalculator.Estimate(UsageUnit.Tokens, 0, 0, Pricing).Should().Be(0m);
    }

    [Fact]
    public void Given_SmallUsage_When_Estimate_Then_RoundsToSixDecimals()
    {
        // 4000 input + 3000 output tokens (typical per-video) → tiny but non-zero.
        var cost = CostCalculator.Estimate(UsageUnit.Tokens, 4000, 3000, Pricing);

        cost.Should().Be(0.0016m); // 0.0004 + 0.0012
    }
}
