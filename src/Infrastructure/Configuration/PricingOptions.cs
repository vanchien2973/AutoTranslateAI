namespace Infrastructure.Configuration;

public sealed class PricingOptions
{
    public const string SectionName = "Pricing";

    public decimal LlmInputPerMillionTokens { get; init; } = 0.10m;
    public decimal LlmOutputPerMillionTokens { get; init; } = 0.40m;
    public decimal TtsPerMillionCharacters { get; init; } = 16.0m;
    public decimal SttPerMinute { get; init; } = 0.0m;

    public UsagePricing ToPricing() =>
        new(LlmInputPerMillionTokens, LlmOutputPerMillionTokens, TtsPerMillionCharacters, SttPerMinute);
}
