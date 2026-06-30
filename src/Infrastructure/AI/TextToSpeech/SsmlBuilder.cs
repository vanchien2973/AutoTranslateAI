using System.Security;

namespace Infrastructure.AI.TextToSpeech;

internal static class SsmlBuilder
{
    public static string RateToPercent(double rateFactor)
    {
        // Clamp to a sane range Azure accepts (also avoids unintelligible speed).
        var clamped = Math.Clamp(rateFactor, 0.5, 3.0);
        var percent = (int)Math.Round((clamped - 1.0) * 100);
        return percent >= 0 ? $"+{percent}%" : $"{percent}%";
    }

    public static string Build(string text, string voiceName, string languageCode, double rateFactor)
    {
        var escaped = SecurityElement.Escape(text) ?? string.Empty;
        var rate = RateToPercent(rateFactor);

        return
            $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"{languageCode}\">" +
            $"<voice name=\"{voiceName}\">" +
            $"<prosody rate=\"{rate}\">{escaped}</prosody>" +
            "</voice></speak>";
    }
}
