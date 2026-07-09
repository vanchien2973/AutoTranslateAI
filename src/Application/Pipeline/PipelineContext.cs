using Domain.Enums;
using Shared.Enums;

namespace Application.Pipeline;

public sealed class PipelineContext
{
    public required Guid JobId { get; init; }
    public required string WorkspacePath { get; init; }
    public required string SourceUrl { get; init; }
    public required string AudioLanguage { get; init; }
    public required string SubtitleLanguage { get; init; }
    public bool EnableDubbing { get; init; } = true;
    public VoiceGender DefaultVoiceGender { get; init; } = VoiceGender.Female;
    public SubtitleMode SubtitleMode { get; init; } = SubtitleMode.None;
    public BgmMode BgmMode { get; init; } = BgmMode.DemucsAI;
    public int DuckingDb { get; init; } = -12;
    public string? SourceLanguage { get; set; }
    public string? SubtitlePath { get; set; }
    public List<PipelineSegment> Segments { get; } = [];
    public string? SourceVideoPath { get; set; }
    public string? AudioPath { get; set; }
    public string? VocalsPath { get; set; }
    public string? BackgroundMusicPath { get; set; }
    public string? DubbedVocalsPath { get; set; }
    public string? MixedAudioPath { get; set; }
    public string? OutputVideoPath { get; set; }
    public string? OutputStorageKey { get; set; }
    public string? OutputUrl { get; set; }
}
