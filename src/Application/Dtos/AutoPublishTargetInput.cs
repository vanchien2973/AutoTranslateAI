using Domain.Enums;

namespace Application.Dtos;

public sealed record AutoPublishTargetInput(
    PublishPlatform Platform,
    Guid? ConnectionId = null,
    string? Title = null,
    string? Description = null);
