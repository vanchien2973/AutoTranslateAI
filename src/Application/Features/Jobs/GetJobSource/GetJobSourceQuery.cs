using MediatR;

namespace Application.Features.Jobs.GetJobSource;

public sealed record GetJobSourceQuery(Guid JobId) : IRequest<GetJobSourceResponse>;
