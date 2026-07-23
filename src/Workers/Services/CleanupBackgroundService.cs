using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Workers.Services;

public sealed class CleanupBackgroundService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CleanupOptions _options;
    private readonly ILogger<CleanupBackgroundService> _logger;

    public CleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<CleanupOptions> options,
        ILogger<CleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Cleanup service disabled");
            return;
        }

        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, _options.RunIntervalHours));
        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Cleanup run failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var jobs = services.GetRequiredService<IDubbingJobRepository>();
        var workspace = services.GetRequiredService<IWorkspaceManager>();
        var janitor = services.GetRequiredService<IWorkspaceJanitor>();
        var storage = services.GetRequiredService<IStorageService>();

        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddDays(-_options.JobRetentionDays);

        var terminal = await jobs.ListTerminalCreatedBeforeAsync(cutoff, cancellationToken);
        var retention = terminal.Select(job => new JobRetentionInfo(job.Id, job.CompletedAt ?? job.UpdatedAt ?? job.CreatedAt));
        var expired = RetentionPolicy.ExpiredJobs(retention, now, _options.JobRetentionDays);

        var logoByJob = terminal
            .Where(job => !string.IsNullOrWhiteSpace(job.LogoStorageKey))
            .ToDictionary(job => job.Id, job => job.LogoStorageKey!);

        foreach (var jobId in expired)
        {
            workspace.Cleanup(jobId);
            try
            {
                await storage.DeleteAsync(OutputStorageKey.For(jobId), cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to delete output object for job {JobId}", jobId);
            }

            if (logoByJob.TryGetValue(jobId, out var logoKey))
            {
                try
                {
                    await storage.DeleteAsync(logoKey, cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Failed to delete logo object for job {JobId}", jobId);
                }
            }
        }

        if (expired.Count > 0)
        {
            await jobs.DeleteAsync(expired, cancellationToken);
            _logger.LogInformation("Cleanup removed {Count} expired jobs (older than {Days}d)", expired.Count, _options.JobRetentionDays);
        }

        // 2) Cắt bớt workspace khi tổng dung lượng vượt ngưỡng (bỏ qua job đang chạy).
        var active = (await jobs.ListActiveJobIdsAsync(cancellationToken)).ToHashSet();
        var toPrune = RetentionPolicy.WorkspacesToPrune(janitor.List(), _options.MaxWorkspaceBytes, active);
        foreach (var path in toPrune)
        {
            janitor.Delete(path);
        }

        if (toPrune.Count > 0)
        {
            _logger.LogInformation("Cleanup pruned {Count} workspaces to stay under {Bytes} bytes", toPrune.Count, _options.MaxWorkspaceBytes);
        }
    }
}
