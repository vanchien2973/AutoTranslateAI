using MediatR;

namespace Application.Features.Jobs.ReopenJob;

public sealed record ReopenJobCommand(Guid JobId) : IRequest<ReopenJobResponse>;
