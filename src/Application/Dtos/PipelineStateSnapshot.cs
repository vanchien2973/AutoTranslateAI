using Application.Pipeline;

namespace Application.Dtos;

public sealed class PipelineStateSnapshot
{
    public string? SourceLanguage { get; set; }
    public List<PipelineSegment> Segments { get; set; } = [];
    public string? SourceVideoPath { get; set; }
    public string? AudioPath { get; set; }
    public string? VocalsPath { get; set; }
    public string? BackgroundMusicPath { get; set; }
    public string? DubbedVocalsPath { get; set; }
    public string? MixedAudioPath { get; set; }
    public string? OutputVideoPath { get; set; }
    public string? OutputStorageKey { get; set; }
    public string? OutputUrl { get; set; }

    public static PipelineStateSnapshot Capture(PipelineContext context) => new()
    {
        SourceLanguage = context.SourceLanguage,
        Segments = [.. context.Segments],
        SourceVideoPath = context.SourceVideoPath,
        AudioPath = context.AudioPath,
        VocalsPath = context.VocalsPath,
        BackgroundMusicPath = context.BackgroundMusicPath,
        DubbedVocalsPath = context.DubbedVocalsPath,
        MixedAudioPath = context.MixedAudioPath,
        OutputVideoPath = context.OutputVideoPath,
        OutputStorageKey = context.OutputStorageKey,
        OutputUrl = context.OutputUrl,
    };

    public void ApplyTo(PipelineContext context)
    {
        context.SourceLanguage = SourceLanguage;
        context.Segments.Clear();
        context.Segments.AddRange(Segments);
        context.SourceVideoPath = SourceVideoPath;
        context.AudioPath = AudioPath;
        context.VocalsPath = VocalsPath;
        context.BackgroundMusicPath = BackgroundMusicPath;
        context.DubbedVocalsPath = DubbedVocalsPath;
        context.MixedAudioPath = MixedAudioPath;
        context.OutputVideoPath = OutputVideoPath;
        context.OutputStorageKey = OutputStorageKey;
        context.OutputUrl = OutputUrl;
    }
}
