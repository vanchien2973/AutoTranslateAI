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

        var sourceLanguage = context.SourceLanguage ?? "auto";
        if (string.Equals(sourceLanguage, context.AudioLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return StepResult.Skip("Audio language equals source language; using original text.");
        }

        var texts = context.Segments.Select(segment => segment.OriginalText).ToList();
        var translated = await _translation.TranslateBatchAsync(
            texts, sourceLanguage, context.AudioLanguage, cancellationToken);

        for (var i = 0; i < context.Segments.Count; i++)
        {
            context.Segments[i].AudioTextAi = translated[i];
        }

        return StepResult.Success();
    }
}
