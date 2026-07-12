using MediatR;

namespace Application.Features.Publishing.GetChannels;

public sealed record GetChannelsQuery : IRequest<GetChannelsResponse>;
