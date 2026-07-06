using MediatR;

namespace Application.Features.Jobs.GetJobStatus;

public sealed record GetJobStatusQuery(Guid JobId) : IRequest<GetJobStatusResponse>;
