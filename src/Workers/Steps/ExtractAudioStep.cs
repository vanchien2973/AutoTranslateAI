using Application.Dtos;
using Application.Interfaces;
using Application.Pipeline;
using Domain.Enums;

namespace Workers.Steps;

/// <summary>Step 2: Separate the audio (16kHz mono WAV) from the source video using ffmpeg.</summary>
public sealed class ExtractAudioStep : IPipelineStep
{
    private readonly IAudioExtractor _extractor;
    private readonly IWorkspaceManager _workspace;

    public ExtractAudioStep(IAudioExtractor extractor, IWorkspaceManager workspace)
    {
        _extractor = extractor;
        _workspace = workspace;
    }

    public StepType StepType => StepType.ExtractAudio;

    public async Task<StepResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.SourceVideoPath))
        {
            return StepResult.Fail("Source video path is not set (Download must run first).");
        }

        var outputPath = _workspace.GetArtifactPath(context.JobId, "audio.wav");
        context.AudioPath = await _extractor.ExtractAudioAsync(
            new AudioExtractionRequest(context.SourceVideoPath, outputPath), cancellationToken);

        return StepResult.Success();
    }
}
