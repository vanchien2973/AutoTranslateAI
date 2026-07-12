using Application.Interfaces;
using Domain.Enums;

namespace Infrastructure.Publishing;

public sealed class OAuthProviderFactory : IOAuthProviderFactory
{
    private readonly IReadOnlyDictionary<PublishPlatform, IOAuthProvider> _providers;

    public OAuthProviderFactory(IEnumerable<IOAuthProvider> providers) =>
        _providers = providers.ToDictionary(provider => provider.Platform);

    public IOAuthProvider Get(PublishPlatform platform) =>
        _providers.TryGetValue(platform, out var provider)
            ? provider
            : throw new NotSupportedException($"No OAuth provider registered for {platform}.");
}
