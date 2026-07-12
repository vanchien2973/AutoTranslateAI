namespace Application.Features.Publishing.GetChannels;

public sealed record GetChannelsResponse(IReadOnlyList<ChannelConnectionDto> Channels);
