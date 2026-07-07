using Domain.Enums;
using Shared.Enums;

namespace Application.Dtos;

public sealed record PipelineRequest(
    Guid JobId,
    string SourceUrl,
    string AudioLanguage,
    string SubtitleLanguage,
    bool EnableDubbing = true,
    VoiceGender DefaultVoiceGender = VoiceGender.Female,
    SubtitleMode SubtitleMode = SubtitleMode.None,
    IReadOnlyList<PipelineSegment>? Segments = null);
