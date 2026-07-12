using Domain.Entities;

namespace Application.Interfaces;

public interface IPublishResultRepository
{
    Task AddAsync(PublishResult result, CancellationToken cancellationToken);

    Task<IReadOnlyList<PublishResult>> ListByJobAsync(Guid jobId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
