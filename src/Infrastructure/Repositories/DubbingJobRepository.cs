using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class DubbingJobRepository : IDubbingJobRepository
{
    private const int MaxConcurrencyRetries = 3;

    private readonly AppDbContext _dbContext;

    public DubbingJobRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<DubbingJob?> GetAsync(Guid jobId, CancellationToken cancellationToken) =>
        _dbContext.DubbingJobs
            .Include(job => job.Segments)
            .Include(job => job.Steps)
            .FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);

    public async Task AddAsync(DubbingJob job, CancellationToken cancellationToken)
    {
        await _dbContext.DubbingJobs.AddAsync(job, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesWithRetryAsync(MaxConcurrencyRetries, cancellationToken);
}
