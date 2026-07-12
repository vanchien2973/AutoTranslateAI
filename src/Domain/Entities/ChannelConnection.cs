using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public sealed class ChannelConnection : BaseEntity
{
    private ChannelConnection()
    {
    }

    public ChannelConnection(
        PublishPlatform platform,
        string channelId,
        string channelName,
        string accessToken,
        string? refreshToken,
        DateTimeOffset? expiresAt)
    {
        Platform = platform;
        ChannelId = channelId;
        ChannelName = channelName;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public PublishPlatform Platform { get; private set; }
    public string ChannelId { get; private set; } = string.Empty;
    public string ChannelName { get; private set; } = string.Empty;
    public string AccessToken { get; private set; } = string.Empty;
    public string? RefreshToken { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void UpdateTokens(string accessToken, string? refreshToken, DateTimeOffset? expiresAt)
    {
        AccessToken = accessToken;
        if (refreshToken is not null)
        {
            RefreshToken = refreshToken;
        }

        ExpiresAt = expiresAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsExpired(DateTimeOffset now) => ExpiresAt is { } expiresAt && expiresAt <= now;
}
