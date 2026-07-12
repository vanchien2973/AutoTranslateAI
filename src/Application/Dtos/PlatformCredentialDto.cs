using Domain.Enums;

namespace Application.Dtos;

public sealed record PlatformCredentialDto(
    PublishPlatform Platform,
    string ClientId,
    bool HasSecret,
    string? DefaultRedirectUri,
    DateTimeOffset? UpdatedAt);
