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
    BgmMode? BgmMode) : IRequest<CreateJobResponse>;
