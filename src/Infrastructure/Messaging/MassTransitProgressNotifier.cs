using Application.Interfaces;
using Application.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public sealed class MassTransitProgressNotifier : IProgressNotifier
{
    private readonly IPublishEndpoint _publish;
    private readonly ILogger<MassTransitProgressNotifier> _logger;

    public MassTransitProgressNotifier(IPublishEndpoint publish, ILogger<MassTransitProgressNotifier> logger)
    {
        _publish = publish;
        _logger = logger;
    }

    public async Task ReportAsync(JobProgress progress, CancellationToken cancellationToken)
    {
        try
        {
            await _publish.Publish(
                new JobProgressUpdated(progress.JobId, progress.Status, progress.CurrentStep, progress.ProgressPercent),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Job {JobId}: failed to publish progress ({Status})", progress.JobId, progress.Status);
        }
    }
}
