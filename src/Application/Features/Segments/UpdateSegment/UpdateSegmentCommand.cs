using System.Text.Json.Serialization;
using MediatR;

namespace Application.Features.Segments.UpdateSegment;

public sealed record UpdateSegmentCommand(
    [property: JsonIgnore] Guid JobId,
    [property: JsonIgnore] Guid SegmentId,
    string? AudioTextEdited,
    string? SubtitleTextEdited,
    string? SpeakerLabel,
    string? AssignedVoice) : IRequest<UpdateSegmentResponse>;
