using Application.Interfaces;
using Application.Messaging;
using Application.Pipeline;
using Domain.Entities;
using Domain.Enums;
using MassTransit;

namespace Workers.Consumers;

public sealed class DubbingJobConsumer : IConsumer<DubbingJobRequested>
{
    private readonly PipelineRunner _runner;
    private readonly IDubbingJobRepository _jobs;
    private readonly ILogger<DubbingJobConsumer> _logger;

    public DubbingJobConsumer(
        PipelineRunner runner,
        IDubbingJobRepository jobs,
        ILogger<DubbingJobConsumer> logger)
    {
        _runner = runner;
        _jobs = jobs;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DubbingJobRequested> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        _logger.LogInformation("Job {JobId}: consuming dubbing request for {Url}", message.JobId, message.SourceUrl);

        var job = await EnsureJobAsync(message, cancellationToken);

        // A redelivered message for an already-finished job is a no-op (idempotent consume).
        if (job.Status is JobStatus.Completed or JobStatus.Cancelled)
        {
            _logger.LogInformation("Job {JobId}: already {Status}, skipping", message.JobId, job.Status);
            return;
        }

        // Idempotent entry into Phase 1: tolerates Queued (fresh), Failed (retry) and ProcessingPhase1 (resume).
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

            var result = await _runner.RunAsync(request, cancellationToken);

            // Single-consumer milestone: auto-advance through the review pause to completion.
            job.MarkAwaitingReview();
            job.Confirm();
            job.StartPhase2();
            job.Complete(result.OutputUrl ?? string.Empty);
            await _jobs.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Job {JobId}: done. Output URL: {Url}", message.JobId, result.OutputUrl);
        }
        catch (Exception ex)
        {
            job.Fail(ex.Message);
            await _jobs.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task<DubbingJob> EnsureJobAsync(DubbingJobRequested message, CancellationToken cancellationToken)
    {
        var existing = await _jobs.GetAsync(message.JobId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var job = new DubbingJob(
            sourceUrl: message.SourceUrl,
            localFilePath: null,
            sourceLanguage: null,
            audioLanguage: message.AudioLanguage,
            subtitleLanguage: message.SubtitleLanguage,
            enableDubbing: message.EnableDubbing);
        await _jobs.AddAsync(job, cancellationToken);
        return job;
    }
}
