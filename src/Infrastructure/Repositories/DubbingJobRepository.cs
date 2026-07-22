using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
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
            .Include(job => job.AutoPublishTargets)
            .FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);

    public async Task AddAsync(DubbingJob job, CancellationToken cancellationToken)
    {
        await _dbContext.DubbingJobs.AddAsync(job, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesWithRetryAsync(MaxConcurrencyRetries, cancellationToken);

    public async Task<(IReadOnlyList<DubbingJob> Items, int TotalCount)> ListAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.DubbingJobs.AsNoTracking().OrderByDescending(job => job.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(job => job.Steps)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<DubbingJob>> ListTerminalCreatedBeforeAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken) =>
        await _dbContext.DubbingJobs
            .AsNoTracking()
            .Where(job => (job.Status == JobStatus.Completed
                    || job.Status == JobStatus.Failed
                    || job.Status == JobStatus.Cancelled)
                && job.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListActiveJobIdsAsync(CancellationToken cancellationToken) =>
        await _dbContext.DubbingJobs
            .AsNoTracking()
            .Where(job => job.Status != JobStatus.Completed
                && job.Status != JobStatus.Failed
                && job.Status != JobStatus.Cancelled)
            .Select(job => job.Id)
            .ToListAsync(cancellationToken);

    public Task<int> DeleteAsync(IReadOnlyList<Guid> jobIds, CancellationToken cancellationToken) =>
        _dbContext.DubbingJobs.Where(job => jobIds.Contains(job.Id)).ExecuteDeleteAsync(cancellationToken);
}
