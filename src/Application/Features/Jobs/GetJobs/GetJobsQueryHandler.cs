using Application.Interfaces;
using MediatR;

namespace Application.Features.Jobs.GetJobs;

public sealed class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, GetJobsResponse>
{
    private readonly IDubbingJobRepository _jobs;

    public GetJobsQueryHandler(IDubbingJobRepository jobs) => _jobs = jobs;

    public async Task<GetJobsResponse> Handle(GetJobsQuery request, CancellationToken cancellationToken)
    {
        var (page, skip, take) = Pagination.Normalize(request.Page, request.PageSize);
        var (jobs, total) = await _jobs.ListAsync(skip, take, cancellationToken);
        var items = jobs.Select(JobMapping.ToSummary).ToList();
        return new GetJobsResponse(new PagedResult<JobSummaryDto>(items, page, take, total));
    }
}
