using Domain.Enums;

namespace Application.Pipeline;

public sealed class PipelineExecutionException : Exception
{
    public PipelineExecutionException(StepType step, string? message)
        : base($"Pipeline failed at step '{step}': {message}")
    {
        Step = step;
    }

    public StepType Step { get; }
}
