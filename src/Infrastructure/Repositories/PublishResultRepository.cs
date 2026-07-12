using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PublishResultRepository : IPublishResultRepository
{
    private readonly AppDbContext _dbContext;

    public PublishResultRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(PublishResult result, CancellationToken cancellationToken) =>
        await _dbContext.PublishResults.AddAsync(result, cancellationToken);

    public async Task<IReadOnlyList<PublishResult>> ListByJobAsync(Guid jobId, CancellationToken cancellationToken) =>
        await _dbContext.PublishResults
            .AsNoTracking()
            .Where(result => result.JobId == jobId)
            .OrderBy(result => result.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
