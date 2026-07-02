using Domain.Enums;

namespace Application.Pipeline;

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
    public double DurationSeconds => EndTime - StartTime;
}
