using MediatR;

namespace Application.Features.Jobs.CancelJob;

public sealed record CancelJobCommand(Guid JobId) : IRequest<CancelJobResponse>;
