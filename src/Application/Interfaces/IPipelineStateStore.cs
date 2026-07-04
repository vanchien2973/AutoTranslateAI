using Application.Pipeline;

namespace Application.Interfaces;

public interface IPipelineStateStore
{
    Task<PipelineStateSnapshot?> LoadAsync(Guid jobId, CancellationToken cancellationToken);

    Task SaveAsync(Guid jobId, PipelineStateSnapshot snapshot, CancellationToken cancellationToken);
}
