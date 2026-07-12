using Domain.Enums;

namespace Application.Dtos;

public sealed record PublishTargetResult(
    PublishPlatform Platform,
    Guid ConnectionId,
    PublishStatus Status,
    string? ExternalId,
    string? Url,
    string? Error);
