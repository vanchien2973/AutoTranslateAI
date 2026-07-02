using Application.Dtos;
using Application.Interfaces;
using Application.Pipeline;
using Domain.Enums;

namespace Workers.Steps;

/// <summary>
/// Step 6: Create voiceovers for each segment and then combine them according to the timestamp to form a single track. Each 
/// segment is read aloud at a natural speed, its length is measured, and then read again with a rate-factor to match the original timeframe.
/// </summary>
public sealed class TtsStep : IPipelineStep
{
    private const double RateTolerance = 0.05;

    private readonly ITtsService _tts;
    private readonly IAudioTimelineAssembler _assembler;
    private readonly IWorkspaceManager _workspace;

    public TtsStep(ITtsService tts, IAudioTimelineAssembler assembler, IWorkspaceManager workspace)
    {
        _tts = tts;
        _assembler = assembler;
        _workspace = workspace;
    }

    public StepType StepType => StepType.Tts;

    public async Task<StepResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        if (!context.EnableDubbing)
        {
            return StepResult.Skip("Dubbing is disabled; keeping original audio.");
        }

        if (context.Segments.Count == 0)
        {
            return StepResult.Skip("No segments to synthesize.");
        }

        var clips = new List<TimelineClip>();
        foreach (var segment in context.Segments)
        {
            var text = SegmentText.ForTts(segment);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var outputPath = _workspace.GetArtifactPath(context.JobId, $"tts/seg-{segment.Index:D4}.wav");
            var gender = segment.Gender ?? context.DefaultVoiceGender;

            // First pass at natural speed to measure duration, then re-synthesize to fit the slot.
            var natural = await _tts.SynthesizeAsync(
                new TtsRequest(text, context.AudioLanguage, gender, segment.AssignedVoice, 1.0, outputPath),
                cancellationToken);

            var rateFactor = RateFactorCalculator.Compute(natural.DurationMs / 1000.0, segment.DurationSeconds);
            if (Math.Abs(rateFactor - 1.0) > RateTolerance)
            {
                await _tts.SynthesizeAsync(
                    new TtsRequest(text, context.AudioLanguage, gender, segment.AssignedVoice, rateFactor, outputPath),
                    cancellationToken);
            }

            segment.TtsAudioPath = outputPath;
            clips.Add(new TimelineClip(outputPath, segment.StartTime));
        }

        if (clips.Count == 0)
        {
            return StepResult.Skip("No segment text to synthesize.");
        }

        var vocalsPath = _workspace.GetArtifactPath(context.JobId, "dubbed_vocals.wav");
        context.DubbedVocalsPath = await _assembler.AssembleAsync(
            new TimelineAssemblyRequest(clips, vocalsPath), cancellationToken);

        return StepResult.Success();
    }
}
