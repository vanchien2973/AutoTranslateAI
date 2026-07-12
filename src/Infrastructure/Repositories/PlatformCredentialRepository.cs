using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PlatformCredentialRepository : IPlatformCredentialRepository
{
    private readonly AppDbContext _dbContext;

    public PlatformCredentialRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<PlatformCredential?> GetAsync(PublishPlatform platform, CancellationToken cancellationToken) =>
        _dbContext.PlatformCredentials.FirstOrDefaultAsync(credential => credential.Platform == platform, cancellationToken);

    public async Task<IReadOnlyList<PlatformCredential>> ListAsync(CancellationToken cancellationToken) =>
        await _dbContext.PlatformCredentials.AsNoTracking().OrderBy(credential => credential.Platform).ToListAsync(cancellationToken);

    public async Task UpsertAsync(PlatformCredential credential, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.PlatformCredentials.AnyAsync(existing => existing.Id == credential.Id, cancellationToken);
        if (!exists)
        {
            await _dbContext.PlatformCredentials.AddAsync(credential, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
