using Application.Interfaces;
using MediatR;

namespace Application.Features.Providers.GetProviders;

public sealed class GetProvidersQueryHandler : IRequestHandler<GetProvidersQuery, GetProvidersResponse>
{
    private readonly IProviderRegistry _providers;

    public GetProvidersQueryHandler(IProviderRegistry providers) => _providers = providers;

    public Task<GetProvidersResponse> Handle(GetProvidersQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(_providers.Current);
}
