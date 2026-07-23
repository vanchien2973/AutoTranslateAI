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
    IReadOnlyList<PipelineSegment>? Segments = null,
    string? LogoStorageKey = null,
    LogoPosition LogoPosition = LogoPosition.BottomRight,
    double LogoScalePercent = 0.1,
    int LogoMargin = 16,
    string? SubtitleFontFamily = null,
    int SubtitleFontSize = 24,
    SubtitlePosition SubtitlePosition = SubtitlePosition.Bottom,
    bool SubtitleBold = false,
    bool SubtitleItalic = false);
