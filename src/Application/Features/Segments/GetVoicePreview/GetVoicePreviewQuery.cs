using MediatR;

namespace Application.Features.Segments.GetVoicePreview;

public sealed record GetVoicePreviewQuery(Guid JobId, Guid SegmentId) : IRequest<GetVoicePreviewResponse>;
