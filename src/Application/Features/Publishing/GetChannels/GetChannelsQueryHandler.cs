using Application.Interfaces;
using MediatR;

namespace Application.Features.Publishing.GetChannels;

public sealed class GetChannelsQueryHandler : IRequestHandler<GetChannelsQuery, GetChannelsResponse>
{
    private readonly IChannelConnectionRepository _connections;

    public GetChannelsQueryHandler(IChannelConnectionRepository connections) => _connections = connections;

    public async Task<GetChannelsResponse> Handle(GetChannelsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var connections = await _connections.ListAsync(cancellationToken);
        return new GetChannelsResponse(connections.Select(connection => connection.ToDto(now)).ToList());
    }
}
