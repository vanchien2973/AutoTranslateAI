using System.Text.Json.Serialization;
using MediatR;

namespace Application.Features.Segments.AdjustSegmentTiming;

public sealed record AdjustSegmentTimingCommand(
    [property: JsonIgnore] Guid JobId,
    [property: JsonIgnore] Guid SegmentId,
    double StartTime,
    double EndTime) : IRequest<AdjustSegmentTimingResponse>;
