using Application.Interfaces;
using Application.Messaging;
using Application.Pipeline;
using Domain.Entities;
using Domain.Enums;
using MassTransit;

namespace Workers.Consumers;

/// <summary>
/// Phase 1: Download → ExtractAudio → SeparateBgm → Transcribe → Translate, set job = AwaitingReview and publish nothing**
/// </summary>
public sealed class Phase1Consumer : IConsumer<DubbingJobRequested>
{
    private readonly PipelineRunner _runner;
    private readonly IDubbingJobRepository _jobs;
    private readonly IProgressNotifier _progress;
    private readonly IJobMetricsMonitor _metrics;
    private readonly ILogger<Phase1Consumer> _logger;

    public Phase1Consumer(
        PipelineRunner runner,
        IDubbingJobRepository jobs,
        IProgressNotifier progress,
        IJobMetricsMonitor metrics,
        ILogger<Phase1Consumer> logger)
    {
        _runner = runner;
        _jobs = jobs;
        _progress = progress;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DubbingJobRequested> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        _logger.LogInformation("Job {JobId}: Phase 1 for {Url}", message.JobId, message.SourceUrl);

        var job = await _jobs.GetAsync(message.JobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId}: not found (deleted?), skipping", message.JobId);
            return;
        }

        // Idempotent: a redelivered message for a job that already finished Phase 1 (or beyond) is a no-op.
        if (job.Status is not (JobStatus.Queued or JobStatus.DownloadingMedia or JobStatus.ProcessingPhase1 or JobStatus.Failed))
        {
            _logger.LogInformation("Job {JobId}: Phase 1 already done ({Status}), skipping", message.JobId, job.Status);
            return;
        }

        job.BeginPhase1Processing();
        await _jobs.SaveChangesAsync(cancellationToken);

        try
        {
            var request = new PipelineRequest(
                message.JobId,
                message.SourceUrl,
                message.AudioLanguage,
                message.SubtitleLanguage,
                message.EnableDubbing);

            var result = await _metrics.TrackAsync(
                job.Id, ct => _runner.RunAsync(request, PipelinePhase.Phase1, ct), cancellationToken);

            // Persist the transcript/translation so the user can review and edit it via the API.
            job.SetSegments(result.Segments.Select(segment => SegmentMapping.ToDomain(job.Id, segment)));

            // Pause: set AwaitingReview and publish NOTHING. The user edits segments then POSTs /confirm.
            job.MarkAwaitingReview();
            await _jobs.SaveChangesAsync(cancellationToken);

            await _progress.ReportAsync(
                new JobProgress(job.Id, nameof(JobStatus.AwaitingReview), null, PipelineProgress.PercentAfter(StepType.Translate)),
                cancellationToken);
            _logger.LogInformation("Job {JobId}: Phase 1 done, awaiting review", message.JobId);
        }
        catch (Exception ex)
        {
            if (job.Status is JobStatus.DownloadingMedia or JobStatus.ProcessingPhase1)
            {
                job.Fail(ex.Message);
                await _jobs.SaveChangesAsync(cancellationToken);
                await _progress.ReportAsync(new JobProgress(job.Id, nameof(JobStatus.Failed), null, 0), cancellationToken);
            }
            else
            {
                _logger.LogWarning(ex, "Job {JobId}: Phase 1 post-processing failure while {Status}; retrying", job.Id, job.Status);
            }

            throw;
        }
    }
}
