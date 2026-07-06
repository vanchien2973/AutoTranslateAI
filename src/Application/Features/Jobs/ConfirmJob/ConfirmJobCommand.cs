using MediatR;

namespace Application.Features.Jobs.ConfirmJob;

public sealed record ConfirmJobCommand(Guid JobId) : IRequest<ConfirmJobResponse>;
