namespace Application.Features.Publishing.GenerateSeoMetadata;

public sealed record GenerateSeoMetadataResponse(SeoStatus Status, SeoMetadata? Metadata, string? Error)
{
    public static GenerateSeoMetadataResponse Ok(SeoMetadata metadata) => new(SeoStatus.Ok, metadata, null);

    public static GenerateSeoMetadataResponse JobNotFound(Guid jobId) =>
        new(SeoStatus.JobNotFound, null, $"Job {jobId} was not found.");

    public static GenerateSeoMetadataResponse GenerationFailed(string error) =>
        new(SeoStatus.GenerationFailed, null, error);
}
