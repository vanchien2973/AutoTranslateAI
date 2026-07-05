using Domain.Enums;

namespace Application.Helpers;

public static class PipelineProgress
{
    private static readonly StepType[] Order =
    [
        StepType.Download,
        StepType.ExtractAudio,
        StepType.SeparateBgm,
        StepType.Transcribe,
        StepType.Translate,
        StepType.Tts,
        StepType.Mix,
        StepType.Render,
        StepType.Upload,
    ];

    public static int TotalSteps => Order.Length;

    public static int PercentAfter(StepType completedStep)
    {
        var index = Array.IndexOf(Order, completedStep);
        return index < 0 ? 0 : (int)Math.Round((index + 1) * 100.0 / Order.Length);
    }
}
