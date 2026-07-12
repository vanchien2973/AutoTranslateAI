using Domain.Enums;

namespace Application.Dtos;

public sealed record PublishTarget(
    PublishPlatform Platform,
    Guid? ConnectionId,
    string? Title,
    string? Description,
    IReadOnlyList<string>? Tags);
