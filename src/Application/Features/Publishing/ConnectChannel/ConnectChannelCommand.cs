using Domain.Enums;
using MediatR;

namespace Application.Features.Publishing.ConnectChannel;

public sealed record ConnectChannelCommand(
    PublishPlatform Platform,
    string Code,
    string RedirectUri) : IRequest<ConnectChannelResponse>;
