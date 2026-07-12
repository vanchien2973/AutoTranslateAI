using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Workers.Steps;

/// <summary>
/// Publish step: upload the rendered video to the platforms. Run this OUTSIDE of PipelineRunner (allows for repeated posting, and won't be skipped when resuming) 
/// but still record the JobStep type Publish for tracking purposes, just like the other steps.
/// </summary>
public sealed class PublishStep : IPublishStep
{
    private readonly IPublishExecutor _executor;
    private readonly IJobStepTracker _stepTracker;
    private readonly ILogger<PublishStep> _logger;

    public PublishStep(IPublishExecutor executor, IJobStepTracker stepTracker, ILogger<PublishStep> logger)
    {
        _executor = executor;
        _stepTracker = stepTracker;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PublishTargetResult>> ExecuteAsync(
        DubbingJob job,
        IReadOnlyList<PublishTarget> targets,
        CancellationToken cancellationToken)
    {
        await _stepTracker.StartAsync(job.Id, StepType.Publish, cancellationToken);
        try
        {
            var results = await _executor.ExecuteAsync(job, targets, cancellationToken);
            await _stepTracker.CompleteAsync(job.Id, StepType.Publish, null, cancellationToken);

            var published = results.Count(result => result.Status == PublishStatus.Published);
            _logger.LogInformation(
                "Job {JobId}: Publish step done — {Published}/{Total} platforms published",
                job.Id, published, results.Count);
            return results;
        }
        catch (Exception exception)
        {
            await _stepTracker.FailAsync(job.Id, StepType.Publish, exception.Message, cancellationToken);
            _logger.LogError(exception, "Job {JobId}: Publish step failed", job.Id);
            throw;
        }
    }
}
