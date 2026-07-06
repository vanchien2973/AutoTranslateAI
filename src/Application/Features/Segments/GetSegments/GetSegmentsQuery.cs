using MediatR;

namespace Application.Features.Segments.GetSegments;

public sealed record GetSegmentsQuery(Guid JobId, int Page, int PageSize) : IRequest<GetSegmentsResponse>;
