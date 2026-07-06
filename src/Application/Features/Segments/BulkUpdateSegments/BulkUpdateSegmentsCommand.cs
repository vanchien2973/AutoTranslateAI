using System.Text.Json.Serialization;
using MediatR;

namespace Application.Features.Segments.BulkUpdateSegments;

public sealed record BulkUpdateSegmentsCommand(
    [property: JsonIgnore] Guid JobId,
    IReadOnlyList<SegmentEdit> Segments) : IRequest<BulkUpdateSegmentsResponse>;
