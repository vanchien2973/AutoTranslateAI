using Application.Dtos;
using Application.Interfaces;
using Application.Pipeline;
using Domain.Enums;

namespace Workers.Steps;

/// <summary>Step 9: Combine the original video with the new audio track (with added voiceover/mixing) to create the output file.</summary>
public sealed class RenderStep : IPipelineStep
{
    private static readonly TimeSpan LogoUrlLifetime = TimeSpan.FromHours(6);
    private readonly IVideoRenderer _renderer;
    private readonly IWorkspaceManager _workspace;
    private readonly IStorageService _storage;

    public RenderStep(IVideoRenderer renderer, IWorkspaceManager workspace, IStorageService storage)
    {
        _renderer = renderer;
        _workspace = workspace;
        _storage = storage;
    }

    public StepType StepType => StepType.Render;

    public async Task<StepResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.SourceVideoPath))
        {
            return StepResult.Fail("No source video to render.");
        }

        var audioPath = context.MixedAudioPath ?? context.DubbedVocalsPath;
        if (string.IsNullOrEmpty(audioPath) && context.EnableDubbing)
        {
            return StepResult.Fail("Dubbing is enabled but no dubbed audio track was produced.");
        }

        var logoUrl = string.IsNullOrWhiteSpace(context.LogoStorageKey)
            ? null
            : await _storage.GetPresignedUrlAsync(context.LogoStorageKey, LogoUrlLifetime, cancellationToken);

        var outputPath = _workspace.GetArtifactPath(context.JobId, "output.mp4");
        context.OutputVideoPath = await _renderer.RenderAsync(
            new RenderRequest(
                context.SourceVideoPath,
                audioPath,
                outputPath,
                context.SubtitleMode,
                context.SubtitlePath,
                logoUrl,
                context.LogoPosition,
                context.LogoScalePercent,
                context.LogoMargin),
            cancellationToken);

        return StepResult.Success();
    }
}
