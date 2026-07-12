using Domain.Enums;

namespace Application.Dtos;

public sealed record PublishResultDto(
    Guid Id,
    PublishPlatform Platform,
    PublishStatus Status,
    string? ExternalId,
    string? Url,
    string? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt);
