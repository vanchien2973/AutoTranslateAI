using Application.Interfaces;
using Domain.Enums;

namespace Infrastructure.Persistence;

public sealed class NullJobStepTracker : IJobStepTracker
{
    private static readonly IReadOnlySet<StepType> Empty = new HashSet<StepType>();
    public Task<IReadOnlySet<StepType>> GetCompletedStepsAsync(Guid jobId, CancellationToken cancellationToken) => Task.FromResult(Empty);
    public Task StartAsync(Guid jobId, StepType stepType, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CompleteAsync(Guid jobId, StepType stepType, string? outputPath, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task FailAsync(Guid jobId, StepType stepType, string error, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task SkipAsync(Guid jobId, StepType stepType, CancellationToken cancellationToken) => Task.CompletedTask;
}
