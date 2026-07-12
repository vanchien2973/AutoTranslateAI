using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Publishing.SetPlatformCredential;

public sealed class SetPlatformCredentialCommandHandler
    : IRequestHandler<SetPlatformCredentialCommand, SetPlatformCredentialResponse>
{
    private readonly IPlatformCredentialRepository _credentials;

    public SetPlatformCredentialCommandHandler(IPlatformCredentialRepository credentials) => _credentials = credentials;

    public async Task<SetPlatformCredentialResponse> Handle(
        SetPlatformCredentialCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _credentials.GetAsync(request.Platform, cancellationToken);
        if (existing is null)
        {
            existing = new PlatformCredential(request.Platform, request.ClientId, request.ClientSecret, request.DefaultRedirectUri);
        }
        else
        {
            existing.Update(request.ClientId, request.ClientSecret, request.DefaultRedirectUri);
        }

        await _credentials.UpsertAsync(existing, cancellationToken);
        return new SetPlatformCredentialResponse(existing.ToDto());
    }
}
