using Application.Interfaces;
using Application.Messaging;
using Domain.Enums;
using MassTransit;

namespace Workers.Consumers;

public sealed class PublishConsumer : IConsumer<DubbingJobPublishRequested>
{
    private readonly IPublishStep _publishStep;
    private readonly IDubbingJobRepository _jobs;
    private readonly IProgressNotifier _progress;
    private readonly ILogger<PublishConsumer> _logger;

    public PublishConsumer(
        IPublishStep publishStep,
        IDubbingJobRepository jobs,
        IProgressNotifier progress,
        ILogger<PublishConsumer> logger)
    {
        _publishStep = publishStep;
        _jobs = jobs;
        _progress = progress;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DubbingJobPublishRequested> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        _logger.LogInformation("Job {JobId}: publish requested ({Count} targets)", message.JobId, message.Targets.Count);

        var job = await _jobs.GetAsync(message.JobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId}: not found for publish, ignoring", message.JobId);
            return;
        }

        if (job.Status != JobStatus.Completed || string.IsNullOrEmpty(job.OutputFilePath))
        {
            _logger.LogInformation("Job {JobId}: publish not applicable ({Status}), skipping", message.JobId, job.Status);
            return;
        }

        var outputPath = job.OutputFilePath;
        job.StartPublishing();
        await _jobs.SaveChangesAsync(cancellationToken);
        await _progress.ReportAsync(
            new JobProgress(job.Id, nameof(JobStatus.Publishing), StepType.Publish.ToString(), 100), cancellationToken);

        try
        {
            await _publishStep.ExecuteAsync(job, message.Targets, cancellationToken);
            job.Complete(outputPath);
            await _jobs.SaveChangesAsync(cancellationToken);
            await _progress.ReportAsync(
                new JobProgress(job.Id, nameof(JobStatus.Completed), null, 100), cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Job {JobId}: publish failed, reverting to Completed for retry", job.Id);
            if (job.Status == JobStatus.Publishing)
            {
                job.Complete(outputPath);
                await _jobs.SaveChangesAsync(cancellationToken);
            }

            throw;
        }
    }
}
