using Application.Features.Providers.GetProviders;
using Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Infrastructure.Configuration;

public sealed class ConfiguredProviderRegistry : IProviderRegistry
{
    private readonly ProviderOptions _options;

    public ConfiguredProviderRegistry(IOptions<ProviderOptions> options) => _options = options.Value;

    public GetProvidersResponse Current =>
        new(_options.Tts, _options.SpeechToText, _options.Translation, _options.Storage);
}
