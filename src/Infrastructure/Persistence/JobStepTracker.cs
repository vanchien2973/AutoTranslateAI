using Application.Interfaces;
using Application.Pipeline;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class JobStepTracker : IJobStepTracker
{
    private const int MaxConcurrencyRetries = 3;

    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public JobStepTracker(IDbContextFactory<AppDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<IReadOnlySet<StepType>> GetCompletedStepsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Finished = Completed or Skipped; both must not re-run on resume. Project only StepType.
        var finished = await db.JobSteps
            .Where(step => step.JobId == jobId
                && (step.Status == JobStepStatus.Completed || step.Status == JobStepStatus.Skipped))
            .Select(step => step.StepType)
            .ToListAsync(cancellationToken);

        return finished.ToHashSet();
    }

    public Task StartAsync(Guid jobId, StepType stepType, CancellationToken cancellationToken) =>
        MutateAsync(jobId, stepType, step => step.Start(), cancellationToken);

    public Task CompleteAsync(Guid jobId, StepType stepType, string? outputPath, CancellationToken cancellationToken) =>
        MutateAsync(jobId, stepType, step => step.Complete(outputPath), cancellationToken);

    public Task FailAsync(Guid jobId, StepType stepType, string error, CancellationToken cancellationToken) =>
        MutateAsync(jobId, stepType, step => step.Fail(error), cancellationToken);

    public Task SkipAsync(Guid jobId, StepType stepType, CancellationToken cancellationToken) =>
        MutateAsync(jobId, stepType, step => step.Skip(), cancellationToken);

    private async Task MutateAsync(Guid jobId, StepType stepType, Action<JobStep> mutate, CancellationToken cancellationToken)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var step = await db.JobSteps
            .FirstOrDefaultAsync(s => s.JobId == jobId && s.StepType == stepType, cancellationToken);

        if (step is null)
        {
            step = new JobStep(jobId, stepType, StepPhases.PhaseOf(stepType));
            await db.JobSteps.AddAsync(step, cancellationToken);
        }

        mutate(step);
        await db.SaveChangesWithRetryAsync(MaxConcurrencyRetries, cancellationToken);
    }
}
