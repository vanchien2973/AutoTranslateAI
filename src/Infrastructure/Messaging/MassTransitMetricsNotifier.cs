using Application.Interfaces;
using Application.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public sealed class MassTransitMetricsNotifier : IMetricsNotifier
{
    private readonly IPublishEndpoint _publish;
    private readonly ILogger<MassTransitMetricsNotifier> _logger;

    public MassTransitMetricsNotifier(IPublishEndpoint publish, ILogger<MassTransitMetricsNotifier> logger)
    {
        _publish = publish;
        _logger = logger;
    }

    public async Task ReportAsync(JobMetrics metrics, CancellationToken cancellationToken)
    {
        try
        {
            await _publish.Publish(
                new JobMetricsUpdated(metrics.JobId, metrics.CpuPercent, metrics.MemoryUsedBytes, metrics.MemoryTotalBytes),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Job {JobId}: failed to publish metrics", metrics.JobId);
        }
    }
}
