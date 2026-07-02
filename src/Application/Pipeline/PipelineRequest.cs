using Domain.Enums;

namespace Application.Pipeline;

public sealed record PipelineRequest(
    Guid JobId,
    string SourceUrl,
    string AudioLanguage,
    string SubtitleLanguage,
    bool EnableDubbing = true,
    VoiceGender DefaultVoiceGender = VoiceGender.Female);
