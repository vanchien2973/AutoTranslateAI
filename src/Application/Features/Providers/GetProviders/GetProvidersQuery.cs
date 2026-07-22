using MediatR;

namespace Application.Features.Providers.GetProviders;

public sealed record GetProvidersQuery : IRequest<GetProvidersResponse>;
