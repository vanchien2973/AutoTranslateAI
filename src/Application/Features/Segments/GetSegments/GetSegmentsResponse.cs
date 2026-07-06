namespace Application.Features.Segments.GetSegments;

public sealed record GetSegmentsResponse(OperationStatus Status, PagedResult<SegmentDto>? Segments)
{
    public static GetSegmentsResponse Ok(PagedResult<SegmentDto> segments) => new(OperationStatus.Ok, segments);

    public static GetSegmentsResponse NotFound() => new(OperationStatus.NotFound, null);
}
