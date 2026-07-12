using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ChannelConnectionRepository : IChannelConnectionRepository
{
    private readonly AppDbContext _dbContext;

    public ChannelConnectionRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<ChannelConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.ChannelConnections.FirstOrDefaultAsync(connection => connection.Id == id, cancellationToken);

    public Task<ChannelConnection?> GetLatestAsync(PublishPlatform platform, CancellationToken cancellationToken) =>
        _dbContext.ChannelConnections
            .Where(connection => connection.Platform == platform)
            .OrderByDescending(connection => connection.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ChannelConnection>> ListAsync(CancellationToken cancellationToken) =>
        await _dbContext.ChannelConnections
            .AsNoTracking()
            .OrderByDescending(connection => connection.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ChannelConnection connection, CancellationToken cancellationToken) =>
        await _dbContext.ChannelConnections.AddAsync(connection, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
