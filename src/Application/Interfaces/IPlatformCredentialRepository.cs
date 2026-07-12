using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IPlatformCredentialRepository
{
    Task<PlatformCredential?> GetAsync(PublishPlatform platform, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformCredential>> ListAsync(CancellationToken cancellationToken);

    Task UpsertAsync(PlatformCredential credential, CancellationToken cancellationToken);
}
