using Application.Interfaces;
using MediatR;

namespace Application.Features.Segments.GetSegments;

public sealed class GetSegmentsQueryHandler : IRequestHandler<GetSegmentsQuery, GetSegmentsResponse>
{
    private readonly IDubbingJobRepository _jobs;

    public GetSegmentsQueryHandler(IDubbingJobRepository jobs) => _jobs = jobs;

    public async Task<GetSegmentsResponse> Handle(GetSegmentsQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return GetSegmentsResponse.NotFound();
        }

        var (page, skip, take) = Pagination.Normalize(request.Page, request.PageSize);
        var ordered = job.Segments.OrderBy(segment => segment.SegmentIndex).ToList();
        var items = ordered.Skip(skip).Take(take).Select(SegmentMapping.ToDto).ToList();

        return GetSegmentsResponse.Ok(new PagedResult<SegmentDto>(items, page, take, ordered.Count));
    }
}
