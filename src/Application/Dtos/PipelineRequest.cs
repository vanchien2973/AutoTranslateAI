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
    BgmMode BgmMode = BgmMode.DemucsAI,
    int DuckingDb = -12,
    IReadOnlyList<PipelineSegment>? Segments = null);
