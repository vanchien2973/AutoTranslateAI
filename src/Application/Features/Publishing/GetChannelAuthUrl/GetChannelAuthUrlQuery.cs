using Domain.Enums;
using MediatR;

namespace Application.Features.Publishing.GetChannelAuthUrl;

public sealed record GetChannelAuthUrlQuery(
    PublishPlatform Platform,
    string RedirectUri,
    string? State) : IRequest<GetChannelAuthUrlResponse>;
