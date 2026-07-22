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
    IReadOnlyList<AutoPublishTargetInput>? AutoPublishTargets = null) : IRequest<CreateJobResponse>;
