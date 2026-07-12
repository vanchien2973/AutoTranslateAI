namespace Application.Dtos;

public sealed record PublishRequest(
    string VideoStorageKey,
    string Title,
    string? Description,
    IReadOnlyList<string> Tags,
    string AccessToken,
    string? ChannelId);
