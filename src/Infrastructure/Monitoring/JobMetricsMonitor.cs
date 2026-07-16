using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Monitoring;

public sealed class JobMetricsMonitor : IJobMetricsMonitor
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    private readonly ISystemMetricsSampler _sampler;
    private readonly IMetricsNotifier _notifier;
    private readonly ILogger<JobMetricsMonitor> _logger;

    public JobMetricsMonitor(
        ISystemMetricsSampler sampler,
        IMetricsNotifier notifier,
        ILogger<JobMetricsMonitor> logger)
    {
        _sampler = sampler;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<T> TrackAsync<T>(Guid jobId, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        using var loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var loop = SampleLoopAsync(jobId, loopCts.Token);
        try
        {
            return await work(cancellationToken);
        }
        finally
        {
            await loopCts.CancelAsync();
            await SafeAwaitAsync(loop);
        }
    }

    private async Task SampleLoopAsync(Guid jobId, CancellationToken cancellationToken)
    {
        try
        {
            var previous = await _sampler.SampleAsync(cancellationToken);
            using var timer = new PeriodicTimer(Interval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var current = await _sampler.SampleAsync(cancellationToken);
                var cpu = CpuPercent(previous, current);
                previous = current;
                await _notifier.ReportAsync(
                    new JobMetrics(jobId, cpu, current.MemoryUsedBytes, current.MemoryTotalBytes),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the tracked work finishes.
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Job {JobId}: metrics loop stopped early", jobId);
        }
    }

    private static double CpuPercent(SystemMetricsSnapshot previous, SystemMetricsSnapshot current)
    {
        var totalDelta = current.CpuTotalJiffies - previous.CpuTotalJiffies;
        var idleDelta = current.CpuIdleJiffies - previous.CpuIdleJiffies;
        if (totalDelta <= 0)
        {
            return 0;
        }

        var usage = 100.0 * (totalDelta - idleDelta) / totalDelta;
        return Math.Clamp(Math.Round(usage, 1), 0, 100);
    }

    private static async Task SafeAwaitAsync(Task task)
    {
        try
        {
            await task;
        }
        catch
        {
            // Loop exceptions are already logged; never surface them from the finally block.
        }
    }
}
