using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class JobStepTracker : IJobStepTracker
{
    private const int MaxConcurrencyRetries = 3;

    private readonly AppDbContext _dbContext;

    public JobStepTracker(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlySet<StepType>> GetCompletedStepsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        // Finished = Completed or Skipped; both must not re-run on resume. Project only StepType.
        var finished = await _dbContext.JobSteps
            .Where(step => step.JobId == jobId
                && (step.Status == JobStepStatus.Completed || step.Status == JobStepStatus.Skipped))
            .Select(step => step.StepType)
            .ToListAsync(cancellationToken);

        return finished.ToHashSet();
    }

    public async Task StartAsync(Guid jobId, StepType stepType, CancellationToken cancellationToken)
    {
        var step = await GetOrCreateAsync(jobId, stepType, cancellationToken);
        step.Start();
        await _dbContext.SaveChangesWithRetryAsync(MaxConcurrencyRetries, cancellationToken);
    }

    public async Task CompleteAsync(Guid jobId, StepType stepType, string? outputPath, CancellationToken cancellationToken)
    {
        var step = await GetOrCreateAsync(jobId, stepType, cancellationToken);
        step.Complete(outputPath);
        await _dbContext.SaveChangesWithRetryAsync(MaxConcurrencyRetries, cancellationToken);
    }

    public async Task FailAsync(Guid jobId, StepType stepType, string error, CancellationToken cancellationToken)
    {
        var step = await GetOrCreateAsync(jobId, stepType, cancellationToken);
        step.Fail(error);
        await _dbContext.SaveChangesWithRetryAsync(MaxConcurrencyRetries, cancellationToken);
    }

    public async Task SkipAsync(Guid jobId, StepType stepType, CancellationToken cancellationToken)
    {
        var step = await GetOrCreateAsync(jobId, stepType, cancellationToken);
        step.Skip();
        await _dbContext.SaveChangesWithRetryAsync(MaxConcurrencyRetries, cancellationToken);
    }

    private async Task<JobStep> GetOrCreateAsync(Guid jobId, StepType stepType, CancellationToken cancellationToken)
    {
        var step = await _dbContext.JobSteps
            .FirstOrDefaultAsync(s => s.JobId == jobId && s.StepType == stepType, cancellationToken);

        if (step is null)
        {
            step = new JobStep(jobId, stepType, PhaseOf(stepType));
            await _dbContext.JobSteps.AddAsync(step, cancellationToken);
        }

        return step;
    }

    // Phase 1 = Download..Translate; Phase 2 = Tts..Publish (matches the 2-phase pipeline split).
    private static int PhaseOf(StepType stepType) => stepType <= StepType.Translate ? 1 : 2;
}
