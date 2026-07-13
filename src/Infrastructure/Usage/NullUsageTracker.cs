using Application.Dtos;
using Application.Interfaces;

namespace Infrastructure.Usage;

public sealed class NullUsageTracker : IUsageTracker
{
    public Task RecordAsync(UsageEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;
}
