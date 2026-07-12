using Application.Interfaces;
using MediatR;

namespace Application.Features.Publishing.GetPlatformCredentials;

public sealed class GetPlatformCredentialsQueryHandler
    : IRequestHandler<GetPlatformCredentialsQuery, GetPlatformCredentialsResponse>
{
    private readonly IPlatformCredentialRepository _credentials;

    public GetPlatformCredentialsQueryHandler(IPlatformCredentialRepository credentials) => _credentials = credentials;

    public async Task<GetPlatformCredentialsResponse> Handle(
        GetPlatformCredentialsQuery request,
        CancellationToken cancellationToken)
    {
        var credentials = await _credentials.ListAsync(cancellationToken);
        return new GetPlatformCredentialsResponse(credentials.Select(credential => credential.ToDto()).ToList());
    }
}
