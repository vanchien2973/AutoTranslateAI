using Application.Dtos;
using Application.Interfaces;
using Application.Pipeline;
using Domain.Enums;

namespace Workers.Steps;

/// <summary>Step 7: Mix the blended voiceover with the background music (ducking) to create the final audio track.</summary>
public sealed class MixStep : IPipelineStep
{
    private readonly IAudioMixer _mixer;
    private readonly IWorkspaceManager _workspace;

    public MixStep(IAudioMixer mixer, IWorkspaceManager workspace)
    {
        _mixer = mixer;
        _workspace = workspace;
    }

    public StepType StepType => StepType.Mix;

    public async Task<StepResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.DubbedVocalsPath))
        {
            return StepResult.Skip("No dubbed vocals to mix (dubbing disabled or no speech).");
        }

        if (string.IsNullOrEmpty(context.BackgroundMusicPath))
        {
            // Nothing to mix against — the dubbed vocals are the final audio track.
            context.MixedAudioPath = context.DubbedVocalsPath;
            return StepResult.Skip("No background music; using dubbed vocals as the final track.");
        }

        var outputPath = _workspace.GetArtifactPath(context.JobId, "mixed.wav");
        context.MixedAudioPath = await _mixer.MixAsync(
            new MixRequest(context.DubbedVocalsPath, context.BackgroundMusicPath, outputPath), cancellationToken);

        return StepResult.Success();
    }
}
