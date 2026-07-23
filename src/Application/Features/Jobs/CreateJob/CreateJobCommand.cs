using Domain.Enums;
using MediatR;
using Shared.Enums;

namespace Application.Features.Jobs.CreateJob;

public sealed record CreateJobCommand(
    string SourceUrl,
    string? AudioLanguage,
    string? SubtitleLanguage,
    bool? EnableDubbing,
    VoiceGender? VoiceGender,
    SubtitleMode? SubtitleMode,
    BgmMode? BgmMode,
    string? LogoStorageKey = null,
    LogoPosition? LogoPosition = null,
    double? LogoScalePercent = null,
    int? LogoMargin = null,
    string? SubtitleFontFamily = null,
    int? SubtitleFontSize = null,
    SubtitlePosition? SubtitlePosition = null,
    bool? SubtitleBold = null,
    bool? SubtitleItalic = null,
    IReadOnlyList<AutoPublishTargetInput>? AutoPublishTargets = null) : IRequest<CreateJobResponse>;
