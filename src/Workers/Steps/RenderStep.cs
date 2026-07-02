using Application.Dtos;
using Application.Interfaces;
using Application.Pipeline;
using Domain.Enums;

namespace Workers.Steps;

/// <summary>Step 8: Combine the original video with the new audio track (with added voiceover/mixing) to create the output file.</summary>
public sealed class RenderStep : IPipelineStep
{
    private readonly IVideoRenderer _renderer;
    private readonly IWorkspaceManager _workspace;

    public RenderStep(IVideoRenderer renderer, IWorkspaceManager workspace)
    {
        _renderer = renderer;
        _workspace = workspace;
    }

    public StepType StepType => StepType.Render;

    public async Task<StepResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.SourceVideoPath))
        {
            return StepResult.Fail("No source video to render.");
        }

        var audioPath = context.MixedAudioPath ?? context.DubbedVocalsPath;
        if (string.IsNullOrEmpty(audioPath))
        {
            return StepResult.Fail("No audio track available to render.");
        }

        var outputPath = _workspace.GetArtifactPath(context.JobId, "output.mp4");
        context.OutputVideoPath = await _renderer.RenderAsync(
            new RenderRequest(context.SourceVideoPath, audioPath, outputPath), cancellationToken);

        return StepResult.Success();
    }
}
