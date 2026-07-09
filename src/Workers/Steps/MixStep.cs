using Application.Interfaces;
using Domain.Enums;

namespace Workers.Steps;

/// <summary>Step 8: Mix the blended voiceover with the background music (ducking) to create the final audio track.</summary>
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

        var plan = BgmPlanner.Resolve(context.BgmMode, context.DuckingDb);
        var backgroundPath = plan.Source switch
        {
            BgmSource.DuckedOriginal => context.AudioPath,          // original audio, ducked under the dub
            BgmSource.DemucsAccompaniment => context.BackgroundMusicPath, // demucs-separated music
            _ => null,                                              // None
        };

        if (string.IsNullOrEmpty(backgroundPath))
        {
            // No background (BGM = None, or the source wasn't produced): dubbed vocals are the final track.
            context.MixedAudioPath = context.DubbedVocalsPath;
            return StepResult.Skip($"No background to mix ({context.BgmMode}); using dubbed vocals as the final track.");
        }

        var outputPath = _workspace.GetArtifactPath(context.JobId, "mixed.wav");
        context.MixedAudioPath = await _mixer.MixAsync(
            new MixRequest(context.DubbedVocalsPath, backgroundPath, outputPath, plan.GainDb), cancellationToken);

        return StepResult.Success();
    }
}
