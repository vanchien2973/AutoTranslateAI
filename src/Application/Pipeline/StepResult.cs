namespace Application.Pipeline;

public sealed record StepResult(StepOutcome Outcome, string? Message = null)
{
    public bool IsSuccess => Outcome == StepOutcome.Success;
    public bool IsSkipped => Outcome == StepOutcome.Skipped;
    public bool IsFailed => Outcome == StepOutcome.Failed;

    public static StepResult Success() => new(StepOutcome.Success);
    public static StepResult Skip(string reason) => new(StepOutcome.Skipped, reason);
    public static StepResult Fail(string error) => new(StepOutcome.Failed, error);
}
