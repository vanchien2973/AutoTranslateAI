using Shared.Enums;

namespace Application.Dtos;

public sealed class PipelineSegment
{
    public required int Index { get; init; }
    public required double StartTime { get; init; }
    public required double EndTime { get; init; }
    public required string OriginalText { get; init; }
    public string? AudioTextAi { get; set; }
    public string? AudioTextEdited { get; set; }
    public string? SubtitleTextAi { get; set; }
    public string? SubtitleTextEdited { get; set; }
    public VoiceGender? Gender { get; set; }
    public string? AssignedVoice { get; set; }
    public string? TtsAudioPath { get; set; }
    public long? TtsDurationMs { get; set; }
    public string? TtsVoice { get; set; }   // the voice the current clip was synthesized with

    // True if the audio text changed after a TTS clip was made — set when seeding from the DB.
    public bool NeedsTtsRegenerate { get; set; }

    public double DurationSeconds => EndTime - StartTime;

    // (Re)synthesize when there is no clip, the text changed, or the desired voice no longer matches the clip's voice.
    public bool NeedsTtsSynthesis =>
        NeedsTtsRegenerate
        || TtsAudioPath is null
        || (AssignedVoice is not null && !string.Equals(AssignedVoice, TtsVoice, StringComparison.Ordinal));
}
