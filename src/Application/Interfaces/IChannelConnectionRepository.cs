using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IChannelConnectionRepository
{
    Task<ChannelConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ChannelConnection?> GetLatestAsync(PublishPlatform platform, CancellationToken cancellationToken);

    Task<ChannelConnection?> GetByChannelAsync(PublishPlatform platform, string channelId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChannelConnection>> ListAsync(CancellationToken cancellationToken);

    Task AddAsync(ChannelConnection connection, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
