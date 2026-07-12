namespace Application.Dtos;

public sealed record ChannelTokens(
    string ChannelId,
    string ChannelName,
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiresAt);
