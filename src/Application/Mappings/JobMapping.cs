using Application.Helpers;
using Domain.Entities;

namespace Application.Mappings;

public static class JobMapping
{
    public static JobSummaryDto ToSummary(DubbingJob job) => new(
        job.Id,
        job.Status.ToString(),
        job.SourceUrl,
        job.CurrentStep?.ToString(),
        JobProgressCalculator.Percent(job),
        job.ErrorMessage,
        job.CreatedAt,
        job.StartedAt,
        job.ReviewReadyAt,
        job.CompletedAt);
}
