using Domain.Entities;

namespace Application.Mappings;

public static class JobMapping
{
    public static JobSummaryDto ToSummary(DubbingJob job) => new(
        job.Id,
        job.Status.ToString(),
        job.SourceUrl,
        job.CurrentStep?.ToString(),
        job.ProgressPercent,
        job.ErrorMessage,
        job.CreatedAt,
        job.StartedAt,
        job.ReviewReadyAt,
        job.CompletedAt);
}
