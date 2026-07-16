namespace Application.Interfaces;

public interface IMetricsNotifier
{
    Task ReportAsync(JobMetrics metrics, CancellationToken cancellationToken);
}
