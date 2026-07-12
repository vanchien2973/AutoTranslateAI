using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public sealed class PublishResult : BaseEntity
{
    private PublishResult()
    {
    }

    public PublishResult(Guid jobId, PublishPlatform platform)
    {
        JobId = jobId;
        Platform = platform;
        Status = PublishStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid JobId { get; private set; }
    public PublishPlatform Platform { get; private set; }
    public PublishStatus Status { get; private set; }
    public string? ExternalId { get; private set; }
    public string? Url { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    public void MarkPublishing() => Status = PublishStatus.Publishing;

    public void MarkPublished(string externalId, string url)
    {
        ExternalId = externalId;
        Url = url;
        ErrorMessage = null;
        Status = PublishStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error)
    {
        ErrorMessage = error;
        Status = PublishStatus.Failed;
    }
}
