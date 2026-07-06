using Application.Enums;
using Domain.Enums;

namespace Application.Helpers;

public static class StepPhases
{
    public static int PhaseOf(StepType stepType) => stepType <= StepType.Translate ? 1 : 2;
    public static bool IsIn(StepType stepType, PipelinePhase phase) => PhaseOf(stepType) == (int)phase;
}
