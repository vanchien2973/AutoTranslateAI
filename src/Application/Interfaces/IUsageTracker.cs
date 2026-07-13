namespace Application.Interfaces;

public interface IUsageTracker
{
    Task RecordAsync(UsageEntry entry, CancellationToken cancellationToken);
}
