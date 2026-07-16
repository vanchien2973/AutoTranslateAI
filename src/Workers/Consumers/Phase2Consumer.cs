using Application.Interfaces;
using Application.Messaging;
using Application.Pipeline;
using Domain.Enums;
using MassTransit;

namespace Workers.Consumers;

/// <summary>
/// Phase 2: run after user Confirm (message DubbingJobConfirmed). Set job = ProcessingPhase2 then run
/// TTS → Mix → Render → Upload
/// </summary>
public sealed class Phase2Consumer : IConsumer<DubbingJobConfirmed>
{
    private readonly PipelineRunner _runner;
    private readonly IDubbingJobRepository _jobs;
    private readonly IProgressNotifier _progress;
    private readonly IJobMetricsMonitor _metrics;
    private readonly ILogger<Phase2Consumer> _logger;

    public Phase2Consumer(
        PipelineRunner runner,
        IDubbingJobRepository jobs,
        IProgressNotifier progress,
        IJobMetricsMonitor metrics,
        ILogger<Phase2Consumer> logger)
    {
        _runner = runner;
        _jobs = jobs;
        _progress = progress;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DubbingJobConfirmed> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        _logger.LogInformation("Job {JobId}: Phase 2 (confirmed)", message.JobId);

        var job = await _jobs.GetAsync(message.JobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId}: not found for Phase 2, ignoring", message.JobId);
            return;
        }

        // Idempotent: a redelivered confirm for an already-finished/cancelled job is a no-op.
        if (job.Status is not (JobStatus.ConfirmedQueued or JobStatus.ProcessingPhase2 or JobStatus.Failed))
        {
            _logger.LogInformation("Job {JobId}: Phase 2 not applicable ({Status}), skipping", message.JobId, job.Status);
            return;
        }

        job.BeginPhase2Processing();
        await _jobs.SaveChangesAsync(cancellationToken);

        try
        {
            var segments = job.Segments
                .OrderBy(segment => segment.SegmentIndex)
                .Select(SegmentMapping.ToPipeline)
                .ToList();

            var request = new PipelineRequest(
                job.Id,
                job.SourceUrl ?? string.Empty,
                job.AudioLanguage,
                job.SubtitleLanguage ?? job.AudioLanguage,
                job.EnableDubbing,
                job.VoiceGender,
                job.SubtitleMode,
                job.BgmMode,
                job.DuckingDb,
                segments);

            var result = await _metrics.TrackAsync(
                job.Id, ct => _runner.RunAsync(request, PipelinePhase.Phase2, ct), cancellationToken);

            // Persist each segment's TTS clip so a future re-review only re-synthesizes the ones that changed.
            var dbSegments = job.Segments.ToDictionary(segment => segment.SegmentIndex);
            foreach (var segment in result.Segments)
            {
                if (segment.TtsAudioPath is not null && dbSegments.TryGetValue(segment.Index, out var dbSegment))
                {
                    dbSegment.SetTtsResult(segment.TtsAudioPath, segment.TtsDurationMs ?? 0, segment.TtsVoice);
                }
            }

            job.Complete(result.OutputUrl ?? string.Empty);
            await _jobs.SaveChangesAsync(cancellationToken);

            await _progress.ReportAsync(new JobProgress(job.Id, nameof(JobStatus.Completed), null, 100), cancellationToken);
            _logger.LogInformation("Job {JobId}: done. Output URL: {Url}", message.JobId, result.OutputUrl);
        }
        catch (Exception ex)
        {
            if (job.Status == JobStatus.ProcessingPhase2)
            {
                job.Fail(ex.Message);
                await _jobs.SaveChangesAsync(cancellationToken);
                await _progress.ReportAsync(new JobProgress(job.Id, nameof(JobStatus.Failed), null, 0), cancellationToken);
            }
            else
            {
                _logger.LogWarning(ex, "Job {JobId}: Phase 2 post-processing failure while {Status}; retrying", job.Id, job.Status);
            }

            throw;
        }
    }
}
