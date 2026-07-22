using Application.Features.Providers.GetProviders;

namespace Application.Interfaces;

public interface IProviderRegistry
{
    GetProvidersResponse Current { get; }
}
