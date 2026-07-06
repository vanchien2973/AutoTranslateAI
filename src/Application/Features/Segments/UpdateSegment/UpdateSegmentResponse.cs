namespace Application.Features.Segments.UpdateSegment;

public sealed record UpdateSegmentResponse(OperationStatus Status, SegmentDto? Segment, string? Error)
{
    public static UpdateSegmentResponse Ok(SegmentDto segment) => new(OperationStatus.Ok, segment, null);

    public static UpdateSegmentResponse NotFound() => new(OperationStatus.NotFound, null, null);

    public static UpdateSegmentResponse Conflict(string error) => new(OperationStatus.Conflict, null, error);
}
