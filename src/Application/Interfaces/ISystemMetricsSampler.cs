namespace Application.Interfaces;

public interface ISystemMetricsSampler
{
    ValueTask<SystemMetricsSnapshot> SampleAsync(CancellationToken cancellationToken);
}
