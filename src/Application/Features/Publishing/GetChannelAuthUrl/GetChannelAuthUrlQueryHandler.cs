using Application.Interfaces;
using MediatR;

namespace Application.Features.Publishing.GetChannelAuthUrl;

public sealed class GetChannelAuthUrlQueryHandler
    : IRequestHandler<GetChannelAuthUrlQuery, GetChannelAuthUrlResponse>
{
    private readonly IPlatformCredentialRepository _credentials;
    private readonly IOAuthProviderFactory _providers;

    public GetChannelAuthUrlQueryHandler(IPlatformCredentialRepository credentials, IOAuthProviderFactory providers)
    {
        _credentials = credentials;
        _providers = providers;
    }

    public async Task<GetChannelAuthUrlResponse> Handle(
        GetChannelAuthUrlQuery request,
        CancellationToken cancellationToken)
    {
        var credential = await _credentials.GetAsync(request.Platform, cancellationToken);
        if (credential is null)
        {
            return GetChannelAuthUrlResponse.CredentialsMissing(request.Platform.ToString());
        }

        var state = string.IsNullOrWhiteSpace(request.State) ? Guid.NewGuid().ToString("N") : request.State;
        var app = new OAuthAppCredentials(credential.ClientId, credential.ClientSecret);
        var url = _providers.Get(request.Platform).BuildAuthorizationUrl(app, request.RedirectUri, state);
        return GetChannelAuthUrlResponse.Ok(url, state);
    }
}
