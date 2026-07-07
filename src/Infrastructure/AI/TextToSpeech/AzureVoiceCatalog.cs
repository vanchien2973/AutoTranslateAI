using Application.Dtos;
using Shared.Enums;

namespace Infrastructure.AI.TextToSpeech;

internal static class AzureVoiceCatalog
{
    private static readonly IReadOnlyDictionary<string, (string Female, string Male)> Defaults =
        new Dictionary<string, (string Female, string Male)>(StringComparer.OrdinalIgnoreCase)
        {
            ["vi"] = ("vi-VN-HoaiMyNeural", "vi-VN-NamMinhNeural"),
            ["en"] = ("en-US-JennyNeural", "en-US-GuyNeural"),
            ["zh-CN"] = ("zh-CN-XiaoxiaoNeural", "zh-CN-YunxiNeural"),
            ["ja"] = ("ja-JP-NanamiNeural", "ja-JP-KeitaNeural"),
            ["ko"] = ("ko-KR-SunHiNeural", "ko-KR-InJoonNeural"),
            ["fr"] = ("fr-FR-DeniseNeural", "fr-FR-HenriNeural"),
            ["es"] = ("es-ES-ElviraNeural", "es-ES-AlvaroNeural"),
        };

    public static readonly IReadOnlyCollection<string> Languages = Defaults.Keys.ToArray();

    public static bool Supports(string languageCode) => TryGetPair(languageCode, out _);

    public static string ResolveVoice(string languageCode, VoiceGender gender)
    {
        if (!TryGetPair(languageCode, out var pair))
        {
            throw new InvalidOperationException(
                $"No default Azure voice configured for language '{languageCode}'.");
        }

        return gender == VoiceGender.Female ? pair.Female : pair.Male;
    }

    public static IReadOnlyList<VoiceInfo> ListVoices(string languageCode)
    {
        if (!TryGetPair(languageCode, out var pair))
        {
            return [];
        }

        return
        [
            new VoiceInfo(pair.Female, languageCode, VoiceGender.Female, pair.Female),
            new VoiceInfo(pair.Male, languageCode, VoiceGender.Male, pair.Male),
        ];
    }

    private static bool TryGetPair(string languageCode, out (string Female, string Male) pair)
    {
        // Match the full BCP-47 code first ("zh-CN"), then the primary subtag ("en-US" -> "en").
        if (Defaults.TryGetValue(languageCode, out pair))
        {
            return true;
        }

        var primary = languageCode.Split('-')[0];
        return Defaults.TryGetValue(primary, out pair);
    }
}
