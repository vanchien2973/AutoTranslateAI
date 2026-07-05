using Domain.Enums;

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

    // True if the audio text changed after a TTS clip was made — set when seeding from the DB.
    public bool NeedsTtsRegenerate { get; set; }

    public double DurationSeconds => EndTime - StartTime;

    // TTS is (re)synthesized only when there is no clip yet or the audio text changed; otherwise reuse the clip.
    public bool NeedsTtsSynthesis => NeedsTtsRegenerate || TtsAudioPath is null;
}
