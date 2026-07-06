namespace Application.Features.Jobs.GetJobs;

public sealed record GetJobsResponse(PagedResult<JobSummaryDto> Jobs);
