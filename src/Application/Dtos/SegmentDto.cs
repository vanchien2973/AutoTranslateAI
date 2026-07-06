namespace Application.Dtos;

public sealed record SegmentDto(
    Guid Id,
    int SegmentIndex,
    double StartTime,
    double EndTime,
    string OriginalText,
    string? AudioTextAi,
    string? AudioTextEdited,
    string? SubtitleTextAi,
    string? SubtitleTextEdited,
    string TtsText,
    string SubtitleText,
    string? AssignedVoice,
    bool IsEdited,
    bool NeedsTtsRegenerate);
