using Application.Dtos;
using Application.Interfaces;
using Application.Pipeline;
using Domain.Enums;

namespace Workers.Steps;

/// <summary>Step 3: Separate the voice and background music using democs.</summary>
public sealed class SeparateBgmStep : IPipelineStep
{
    private readonly IDemucsService _demucs;
    private readonly IWorkspaceManager _workspace;

    public SeparateBgmStep(IDemucsService demucs, IWorkspaceManager workspace)
    {
        _demucs = demucs;
        _workspace = workspace;
    }

    public StepType StepType => StepType.SeparateBgm;

    public async Task<StepResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.AudioPath))
        {
            return StepResult.Fail("Audio path is not set (ExtractAudio must run first).");
        }

        var stemsDirectory = _workspace.GetArtifactPath(context.JobId, "stems");
        var result = await _demucs.SeparateAsync(
            new DemucsRequest(context.AudioPath, stemsDirectory), cancellationToken);

        context.VocalsPath = result.VocalsPath;
        context.BackgroundMusicPath = result.AccompanimentPath;
        return StepResult.Success();
    }
}
