using MediatR;

namespace Application.Features.Jobs.GetJobs;

public sealed record GetJobsQuery(int Page, int PageSize) : IRequest<GetJobsResponse>;
