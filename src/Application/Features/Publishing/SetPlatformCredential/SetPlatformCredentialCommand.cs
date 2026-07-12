using System.Text.Json.Serialization;
using Domain.Enums;
using MediatR;

namespace Application.Features.Publishing.SetPlatformCredential;

public sealed record SetPlatformCredentialCommand(
    [property: JsonIgnore] PublishPlatform Platform,
    string ClientId,
    string ClientSecret,
    string? DefaultRedirectUri) : IRequest<SetPlatformCredentialResponse>;
