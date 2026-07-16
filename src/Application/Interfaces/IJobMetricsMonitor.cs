namespace Application.Interfaces;

public interface IJobMetricsMonitor
{
    Task<T> TrackAsync<T>(Guid jobId, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);
}
