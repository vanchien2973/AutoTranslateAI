using Domain.Enums;

namespace Application.Pipeline;

public interface IPipelineStep
{
    StepType StepType { get; }

    Task<StepResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken);
}
