using Domain.Entities;

namespace Application.Interfaces;

public interface IDubbingJobRepository
{
    Task<DubbingJob?> GetAsync(Guid jobId, CancellationToken cancellationToken);

    Task AddAsync(DubbingJob job, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
