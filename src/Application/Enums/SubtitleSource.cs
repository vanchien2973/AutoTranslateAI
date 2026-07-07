namespace Application.Enums;

public enum SubtitleSource
{
    None,        // No subtitle track.
    Original,    // Subtitle = source language → use the original transcript.
    ReuseAudio,  // Subtitle = audio language (already translated) → reuse the audio translation.
    Translate,   // Subtitle needs its own LLM translation.
}
