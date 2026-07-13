namespace Application.Dtos;

public sealed record UsagePricing(
    decimal LlmInputPerMillionTokens,
    decimal LlmOutputPerMillionTokens,
    decimal TtsPerMillionCharacters,
    decimal SttPerMinute);
