using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public sealed class PlatformCredential : BaseEntity
{
    private PlatformCredential()
    {
    }

    public PlatformCredential(
        PublishPlatform platform,
        string clientId,
        string clientSecret,
        string? defaultRedirectUri)
    {
        Platform = platform;
        ClientId = clientId;
        ClientSecret = clientSecret;
        DefaultRedirectUri = defaultRedirectUri;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public PublishPlatform Platform { get; private set; }
    public string ClientId { get; private set; } = string.Empty;
    public string ClientSecret { get; private set; } = string.Empty;
    public string? DefaultRedirectUri { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(string clientId, string clientSecret, string? defaultRedirectUri)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        DefaultRedirectUri = defaultRedirectUri;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
