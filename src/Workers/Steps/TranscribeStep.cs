using Application.Dtos;
using Application.Interfaces;
using Application.Pipeline;
using Domain.Enums;

namespace Workers.Steps;

/// <summary>Step 4: Identify the speech and divide it into segments with timestamps (whisper).</summary>
public sealed class TranscribeStep : IPipelineStep
{
    private readonly ISpeechToTextService _speechToText;
    private readonly IAudioExtractor _audioExtractor;
    private readonly IWorkspaceManager _workspace;

    public TranscribeStep(
        ISpeechToTextService speechToText,
        IAudioExtractor audioExtractor,
        IWorkspaceManager workspace)
    {
        _speechToText = speechToText;
        _audioExtractor = audioExtractor;
        _workspace = workspace;
    }

    public StepType StepType => StepType.Transcribe;

    public async Task<StepResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        // Transcribe the separated vocals when available (cleaner), otherwise the full audio.
        var sourceAudio = context.VocalsPath ?? context.AudioPath;
        if (string.IsNullOrEmpty(sourceAudio))
        {
            return StepResult.Fail("No audio available to transcribe.");
        }

        // whisper.net requires 16 kHz mono; demucs vocals are 44.1 kHz, so resample first.
        var whisperInput = _workspace.GetArtifactPath(context.JobId, "transcribe-16k.wav");
        await _audioExtractor.ExtractAudioAsync(
            new AudioExtractionRequest(sourceAudio, whisperInput), cancellationToken);

        var result = await _speechToText.TranscribeAsync(whisperInput, context.SourceLanguage, cancellationToken);
        context.SourceLanguage ??= result.DetectedLanguage;

        context.Segments.Clear();
        foreach (var segment in result.Segments)
        {
            context.Segments.Add(new PipelineSegment
            {
                Index = segment.Index,
                StartTime = segment.StartTime,
                EndTime = segment.EndTime,
                OriginalText = segment.Text,
            });
        }

        return StepResult.Success();
    }
}
