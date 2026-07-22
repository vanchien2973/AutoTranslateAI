using Domain.Entities;
using Domain.Enums;

namespace Application.Helpers;

public static class JobProgressCalculator
{
    private const int TotalPipelineSteps = 9;

    public static int Percent(DubbingJob job)
    {
        if (job.Status == JobStatus.Completed)
        {
            return 100;
        }

        var finished = job.Steps.Count(step => step.Status is JobStepStatus.Completed or JobStepStatus.Skipped);
        return Math.Min(99, finished * 100 / TotalPipelineSteps);
    }
}
