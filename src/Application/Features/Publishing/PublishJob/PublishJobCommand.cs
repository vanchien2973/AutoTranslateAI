using System.Text.Json.Serialization;
using MediatR;

namespace Application.Features.Publishing.PublishJob;

public sealed record PublishJobCommand(
    [property: JsonIgnore] Guid JobId,
    IReadOnlyList<PublishTarget> Targets) : IRequest<PublishJobResponse>;
