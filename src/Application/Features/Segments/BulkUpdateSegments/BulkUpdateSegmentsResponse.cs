namespace Application.Features.Segments.BulkUpdateSegments;

public sealed record BulkUpdateSegmentsResponse(OperationStatus Status, IReadOnlyList<SegmentDto>? Segments, string? Error)
{
    public static BulkUpdateSegmentsResponse Ok(IReadOnlyList<SegmentDto> segments) => new(OperationStatus.Ok, segments, null);

    public static BulkUpdateSegmentsResponse NotFound(string error) => new(OperationStatus.NotFound, null, error);

    public static BulkUpdateSegmentsResponse Conflict(string error) => new(OperationStatus.Conflict, null, error);
}
