using Application.Dtos;
using Domain.Entities;

namespace Application.Mappings;

public static class PublishingMapping
{
    public static PlatformCredentialDto ToDto(this PlatformCredential credential) =>
        new(
            credential.Platform,
            credential.ClientId,
            !string.IsNullOrEmpty(credential.ClientSecret),
            credential.DefaultRedirectUri,
            credential.UpdatedAt);

    public static ChannelConnectionDto ToDto(this ChannelConnection connection, DateTimeOffset now) =>
        new(
            connection.Id,
            connection.Platform,
            connection.ChannelId,
            connection.ChannelName,
            connection.IsExpired(now),
            connection.CreatedAt);

    public static PublishResultDto ToDto(this PublishResult result) =>
        new(
            result.Id,
            result.Platform,
            result.Status,
            result.ExternalId,
            result.Url,
            result.ErrorMessage,
            result.CreatedAt,
            result.PublishedAt);
}
