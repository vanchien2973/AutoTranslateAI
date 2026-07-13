using Domain.Entities;

namespace Application.Interfaces;

public interface IUsageRepository
{
    Task<IReadOnlyList<UsageRecord>> ListSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);
}
