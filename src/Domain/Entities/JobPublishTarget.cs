using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public sealed class JobPublishTarget : BaseEntity
{
    public const int MaxTitleLength = 100;

    private JobPublishTarget()
    {
    }

    public JobPublishTarget(
        Guid jobId,
        PublishPlatform platform,
        Guid? connectionId = null,
        string? title = null,
        string? description = null)
    {
        if (title is { Length: > MaxTitleLength })
        {
            throw new BusinessRuleViolationException(
                $"Publish title must be {MaxTitleLength} characters or fewer.");
        }

        JobId = jobId;
        Platform = platform;
        ConnectionId = connectionId;
        Title = title;
        Description = description;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid JobId { get; private set; }
    public PublishPlatform Platform { get; private set; }

    public Guid? ConnectionId { get; private set; }

    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
