namespace Application.Features.Publishing.PublishJob;

public sealed record PublishJobResponse(PublishJobStatus Status, string? Error)
{
    public static PublishJobResponse Ok() => new(PublishJobStatus.Ok, null);

    public static PublishJobResponse JobNotFound(Guid jobId) =>
        new(PublishJobStatus.JobNotFound, $"Job {jobId} was not found.");

    public static PublishJobResponse NotCompleted() =>
        new(PublishJobStatus.NotCompleted, "The job must finish rendering before it can be published.");

    public static PublishJobResponse NoTargets() =>
        new(PublishJobStatus.NoTargets, "At least one publish target is required.");
}
