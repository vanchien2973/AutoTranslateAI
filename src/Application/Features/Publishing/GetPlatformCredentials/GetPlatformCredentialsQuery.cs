using MediatR;

namespace Application.Features.Publishing.GetPlatformCredentials;

public sealed record GetPlatformCredentialsQuery : IRequest<GetPlatformCredentialsResponse>;
