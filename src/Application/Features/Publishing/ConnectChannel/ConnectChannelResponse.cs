using Application.Dtos;
using Application.Enums;

namespace Application.Features.Publishing.ConnectChannel;

public sealed record ConnectChannelResponse(ConnectChannelStatus Status, ChannelConnectionDto? Connection, string? Error)
{
    public static ConnectChannelResponse Ok(ChannelConnectionDto connection) =>
        new(ConnectChannelStatus.Ok, connection, null);

    public static ConnectChannelResponse CredentialsMissing(string platform) =>
        new(ConnectChannelStatus.CredentialsMissing, null, $"No app credentials configured for {platform}.");

    public static ConnectChannelResponse ExchangeFailed(string error) =>
        new(ConnectChannelStatus.ExchangeFailed, null, error);
}
