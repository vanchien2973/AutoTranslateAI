namespace Application.Dtos;

public sealed record TranslationPlan(bool TranslateAudio, SubtitleSource Subtitle)
{
    public int TranslationCalls =>
        (TranslateAudio ? 1 : 0) + (Subtitle == SubtitleSource.Translate ? 1 : 0);
}
