using Domain.Enums;

namespace Application.Helpers;

public static class CostCalculator
{
    private const decimal Million = 1_000_000m;

    public static decimal Estimate(UsageUnit unit, long inputUnits, long outputUnits, UsagePricing pricing)
    {
        var cost = unit switch
        {
            UsageUnit.Tokens =>
                inputUnits / Million * pricing.LlmInputPerMillionTokens
                + outputUnits / Million * pricing.LlmOutputPerMillionTokens,
            UsageUnit.Characters => inputUnits / Million * pricing.TtsPerMillionCharacters,
            UsageUnit.Seconds => inputUnits / 60m * pricing.SttPerMinute,
            _ => 0m,
        };

        return Math.Round(cost, 6, MidpointRounding.AwayFromZero);
    }
}
