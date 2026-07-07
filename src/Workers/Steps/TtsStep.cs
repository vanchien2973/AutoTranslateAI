using Application.Dtos;
using Application.Interfaces;
using Application.Pipeline;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Workers.Steps;

/// <summary>
/// Step 7: Create voiceovers for each segment and then combine them according to the timestamp to form a single track. Each 
/// segment is read aloud at a natural speed, its length is measured, and then read again with a rate-factor to match the original timeframe.
/// </summary>
public sealed class TtsStep : IPipelineStep
{
    private const double RateTolerance = 0.05;

    private readonly ITtsService _tts;
    private readonly IAudioTimelineAssembler _assembler;
    private readonly IWorkspaceManager _workspace;
    private readonly ILogger<TtsStep> _logger;

    public TtsStep(
        ITtsService tts,
        IAudioTimelineAssembler assembler,
        IWorkspaceManager workspace,
        ILogger<TtsStep> logger)
    {
        _tts = tts;
        _assembler = assembler;
        _workspace = workspace;
        _logger = logger;
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

        var synthesized = 0;
        var reused = 0;
        var clips = new List<TimelineClip>();
        var ordered = context.Segments.OrderBy(segment => segment.StartTime).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var segment = ordered[i];
            var text = SegmentText.ForTts(segment);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var outputPath = _workspace.GetArtifactPath(context.JobId, $"tts/seg-{segment.Index:D4}.wav");

            // Skip the (paid) TTS calls when this segment already has a clip and its audio text didn't change.
            if (!segment.NeedsTtsSynthesis)
            {
                reused++;
                _logger.LogInformation("Job {JobId}: TTS reuse seg {Index} (unchanged)", context.JobId, segment.Index);
                clips.Add(new TimelineClip(segment.TtsAudioPath!, segment.StartTime));
                continue;
            }

            synthesized++;
            _logger.LogInformation("Job {JobId}: TTS synth seg {Index}", context.JobId, segment.Index);
            var gender = segment.Gender ?? context.DefaultVoiceGender;

            // First pass at natural speed to measure duration, then re-synthesize to fit the slot.
            var natural = await _tts.SynthesizeAsync(
                new TtsRequest(text, context.AudioLanguage, gender, segment.AssignedVoice, 1.0, outputPath),
                cancellationToken);
            var durationMs = natural.DurationMs;

            // Borrow trailing silence up to the next segment's start so long speech isn't over-compressed.
            var nextStart = i < ordered.Count - 1 ? ordered[i + 1].StartTime : double.PositiveInfinity;
            var timing = RateFactorCalculator.Fit(segment.StartTime, segment.EndTime, natural.DurationMs / 1000.0, nextStart);
            if (Math.Abs(timing.RateFactor - 1.0) > RateTolerance)
            {
                var adjusted = await _tts.SynthesizeAsync(
                    new TtsRequest(text, context.AudioLanguage, gender, segment.AssignedVoice, timing.RateFactor, outputPath),
                    cancellationToken);
                durationMs = adjusted.DurationMs;
            }

            segment.TtsAudioPath = outputPath;
            segment.TtsDurationMs = durationMs;
            segment.NeedsTtsRegenerate = false;
            clips.Add(new TimelineClip(outputPath, segment.StartTime));
        }

        if (clips.Count == 0)
        {
            return StepResult.Skip("No segment text to synthesize.");
        }

        _logger.LogInformation("Job {JobId}: TTS synthesized {Synth}, reused {Reused}", context.JobId, synthesized, reused);

        var vocalsPath = _workspace.GetArtifactPath(context.JobId, "dubbed_vocals.wav");
        context.DubbedVocalsPath = await _assembler.AssembleAsync(
            new TimelineAssemblyRequest(clips, vocalsPath), cancellationToken);

        return StepResult.Success();
    }
}
