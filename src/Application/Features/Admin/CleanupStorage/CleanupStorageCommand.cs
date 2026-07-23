using MediatR;

namespace Application.Features.Admin.CleanupStorage;

public sealed record CleanupStorageCommand(bool DryRun) : IRequest<CleanupStorageResponse>;
