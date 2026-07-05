namespace Application.Dtos;

public sealed record UpdateSegmentRequest(
    string? AudioTextEdited,
    string? SubtitleTextEdited,
    string? AssignedVoice);

public sealed record BulkUpdateSegmentsRequest(IReadOnlyList<SegmentEdit> Segments);

public sealed record SegmentEdit(
    Guid SegmentId,
    string? AudioTextEdited,
    string? SubtitleTextEdited,
    string? AssignedVoice);

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
