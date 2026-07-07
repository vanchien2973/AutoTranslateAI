namespace Application.Helpers;

public static class TranslationPlanner
{
    public static TranslationPlan Plan(
        string? sourceLanguage,
        string audioLanguage,
        string? subtitleLanguage,
        bool enableDubbing)
    {
        // When dubbing is off the audio track stays original, so the effective audio language is the source.
        var translateAudio = enableDubbing && !SameLanguage(audioLanguage, sourceLanguage);
        var effectiveAudio = enableDubbing ? audioLanguage : sourceLanguage;

        return new TranslationPlan(translateAudio, ResolveSubtitle(sourceLanguage, effectiveAudio, subtitleLanguage, translateAudio));
    }

    private static SubtitleSource ResolveSubtitle(string? source, string? effectiveAudio, string? subtitle, bool audioTranslated)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
        {
            return SubtitleSource.None;
        }

        if (SameLanguage(subtitle, source))
        {
            return SubtitleSource.Original;
        }

        // Only reuse the audio translation when there actually is one.
        if (audioTranslated && SameLanguage(subtitle, effectiveAudio))
        {
            return SubtitleSource.ReuseAudio;
        }

        return SubtitleSource.Translate;
    }

    private static bool SameLanguage(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a)
        && !string.IsNullOrWhiteSpace(b)
        && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
