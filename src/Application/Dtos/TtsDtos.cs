using Shared.Enums;

namespace Application.Dtos;

public sealed record TtsRequest(
    string Text,
    string LanguageCode,            // BCP-47: "vi", "en", "zh-CN"...
    VoiceGender Gender,
    string? VoiceId = null,         // null = pick a default voice from language + gender
    double RateFactor = 1.0,        // >1 speeds up, <1 slows down, to fit the original timestamp
    string OutputPath = "");
public sealed record TtsResult(string AudioPath, long DurationMs, string VoiceUsed);
public sealed record VoiceInfo(string VoiceId, string LanguageCode, VoiceGender Gender, string DisplayName);
