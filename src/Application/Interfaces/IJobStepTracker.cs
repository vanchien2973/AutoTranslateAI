using Domain.Enums;

namespace Application.Interfaces;

public interface IJobStepTracker
{
    Task<IReadOnlySet<StepType>> GetCompletedStepsAsync(Guid jobId, CancellationToken cancellationToken);

    Task StartAsync(Guid jobId, StepType stepType, CancellationToken cancellationToken);

    Task CompleteAsync(Guid jobId, StepType stepType, string? outputPath, CancellationToken cancellationToken);

    Task FailAsync(Guid jobId, StepType stepType, string error, CancellationToken cancellationToken);

    Task SkipAsync(Guid jobId, StepType stepType, CancellationToken cancellationToken);
}
