namespace Application.Features.Segments.AdjustSegmentTiming;

public sealed record AdjustSegmentTimingResponse(OperationStatus Status, SegmentDto? Segment, string? Error)
{
    public static AdjustSegmentTimingResponse Ok(SegmentDto segment) => new(OperationStatus.Ok, segment, null);

    public static AdjustSegmentTimingResponse NotFound() => new(OperationStatus.NotFound, null, null);

    public static AdjustSegmentTimingResponse Conflict(string error) => new(OperationStatus.Conflict, null, error);
}
