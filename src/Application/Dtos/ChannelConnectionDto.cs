using Domain.Enums;

namespace Application.Dtos;

public sealed record ChannelConnectionDto(
    Guid Id,
    PublishPlatform Platform,
    string ChannelId,
    string ChannelName,
    bool IsExpired,
    DateTimeOffset ConnectedAt);
