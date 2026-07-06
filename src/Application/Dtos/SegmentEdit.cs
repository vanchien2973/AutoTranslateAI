namespace Application.Dtos;

public sealed record SegmentEdit(
    Guid SegmentId,
    string? AudioTextEdited,
    string? SubtitleTextEdited,
    string? AssignedVoice);
