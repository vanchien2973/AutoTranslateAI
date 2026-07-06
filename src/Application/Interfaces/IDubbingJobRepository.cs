using Domain.Entities;

namespace Application.Interfaces;

public interface IDubbingJobRepository
{
    Task<DubbingJob?> GetAsync(Guid jobId, CancellationToken cancellationToken);

    Task AddAsync(DubbingJob job, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<(IReadOnlyList<DubbingJob> Items, int TotalCount)> ListAsync(int skip, int take, CancellationToken cancellationToken);
}
