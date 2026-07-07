using Application.Enums;
using Application.Helpers;
using Application.Interfaces;
using Application.Pipeline;
using Domain.Enums;

namespace Workers.Steps;

/// <summary>
/// Step 5: Translate the text segments into the audio language. Skip this step if the source language matches 
/// the audio language (no need to call LLM — saves costs). Subtitle translation will be handled in the GenSubtitle step later.
/// </summary>
public sealed class TranslateStep : IPipelineStep
{
    private readonly ITranslationService _translation;

    public TranslateStep(ITranslationService translation) => _translation = translation;

    public StepType StepType => StepType.Translate;

    public async Task<StepResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        if (context.Segments.Count == 0)
        {
            return StepResult.Skip("No segments to translate.");
        }

        var plan = TranslationPlanner.Plan(
            context.SourceLanguage, context.AudioLanguage, context.SubtitleLanguage, context.EnableDubbing);

        if (plan.TranslationCalls == 0)
        {
            return StepResult.Skip("No translation needed for this language combination.");
        }

        var sourceLanguage = context.SourceLanguage ?? "auto";
        var texts = context.Segments.Select(segment => segment.OriginalText).ToList();

        if (plan.TranslateAudio)
        {
            var audio = await _translation.TranslateBatchAsync(texts, sourceLanguage, context.AudioLanguage, cancellationToken);
            for (var i = 0; i < context.Segments.Count; i++)
            {
                context.Segments[i].AudioTextAi = audio[i];
            }
        }

        switch (plan.Subtitle)
        {
            case SubtitleSource.Translate:
                var subtitle = await _translation.TranslateBatchAsync(
                    texts, sourceLanguage, context.SubtitleLanguage!, cancellationToken);
                for (var i = 0; i < context.Segments.Count; i++)
                {
                    context.Segments[i].SubtitleTextAi = subtitle[i];
                }

                break;

            case SubtitleSource.Original when plan.TranslateAudio:
                // Audio is dubbed but the subtitle stays in the source language: pin it to the original so it
                // doesn't fall back to the audio translation via the COALESCE resolution.
                foreach (var segment in context.Segments)
                {
                    segment.SubtitleTextAi = segment.OriginalText;
                }

                break;
        }

        return StepResult.Success();
    }
}
