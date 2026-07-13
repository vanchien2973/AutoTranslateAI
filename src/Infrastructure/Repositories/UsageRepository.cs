using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class UsageRepository : IUsageRepository
{
    private readonly AppDbContext _dbContext;

    public UsageRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<UsageRecord>> ListSinceAsync(DateTimeOffset since, CancellationToken cancellationToken) =>
        await _dbContext.UsageRecords
            .AsNoTracking()
            .Where(record => record.CreatedAt >= since)
            .OrderBy(record => record.CreatedAt)
            .ToListAsync(cancellationToken);
}
