using Application.Interfaces;
using Domain.Enums;
using Infrastructure.Configuration;
using Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using ChatMessage = OpenAI.Chat.ChatMessage;

namespace Infrastructure.AI.Translation;

public sealed class OpenAiTranslationService : ITranslationService
{
    private readonly ChatClient _client;
    private readonly string _model;
    private readonly ExternalApiResiliencePipeline _resilience;
    private readonly IUsageTracker _usage;
    private readonly ILogger<OpenAiTranslationService> _logger;

    public OpenAiTranslationService(
        IOptions<OpenAIOptions> options,
        ExternalApiResiliencePipeline resilience,
        IUsageTracker usage,
        ILogger<OpenAiTranslationService> logger)
    {
        var config = options.Value;
        _model = config.Model;
        _client = new ChatClient(_model, config.ApiKey);
        _resilience = resilience;
        _usage = usage;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> TranslateBatchAsync(
        IReadOnlyList<string> texts,
        string sourceLang,
        string targetLang,
        CancellationToken cancellationToken)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        var billable = new List<(int Index, string Text)>(texts.Count);
        for (var i = 0; i < texts.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(texts[i]))
            {
                billable.Add((i, texts[i]));
            }
        }

        var result = texts.ToArray();
        if (billable.Count == 0)
        {
            return result;
        }

        _logger.LogInformation(
            "Translating {Count} segments {Source}->{Target} with {Model}",
            billable.Count, sourceLang, targetLang, _model);

        var translated = await TranslateChunkAsync(
            billable.Select(entry => entry.Text).ToList(), sourceLang, targetLang, cancellationToken);

        for (var i = 0; i < billable.Count; i++)
        {
            result[billable[i].Index] = translated[i];
        }

        return result;
    }

    private async Task<IReadOnlyList<string>> TranslateChunkAsync(
        IReadOnlyList<string> texts,
        string sourceLang,
        string targetLang,
        CancellationToken cancellationToken)
    {
        var content = await CompleteAsync(texts, sourceLang, targetLang, cancellationToken);
        if (TranslationResponseParser.TryParse(content, texts.Count, out var translations))
        {
            return translations;
        }

        if (texts.Count == 1)
        {
            _logger.LogWarning("Translation returned an unusable shape for a single segment; keeping original text.");
            return [texts[0]];
        }

        _logger.LogWarning(
            "Translation count mismatch for {Count} segments; splitting the batch and retrying.", texts.Count);

        var mid = texts.Count / 2;
        var left = await TranslateChunkAsync([.. texts.Take(mid)], sourceLang, targetLang, cancellationToken);
        var right = await TranslateChunkAsync([.. texts.Skip(mid)], sourceLang, targetLang, cancellationToken);
        return [.. left, .. right];
    }

    private async Task<string> CompleteAsync(
        IReadOnlyList<string> texts,
        string sourceLang,
        string targetLang,
        CancellationToken cancellationToken)
    {
        ChatMessage[] messages =
        [
            new SystemChatMessage(TranslationPromptBuilder.BuildSystemPrompt(sourceLang, targetLang)),
            new UserChatMessage(TranslationPromptBuilder.BuildUserPrompt(texts)),
        ];

        var chatOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
        };

        // Retry transient failures (429/5xx/timeout) with back-off via the shared Polly pipeline.
        var completion = await _resilience.Pipeline.ExecuteAsync(
            async ct => await _client.CompleteChatAsync(messages, chatOptions, ct),
            cancellationToken);

        var tokens = completion.Value.Usage;
        await _usage.RecordAsync(
            new UsageEntry("OpenAI", "Translate", UsageUnit.Tokens, tokens.InputTokenCount, tokens.OutputTokenCount),
            cancellationToken);

        return completion.Value.Content[0].Text;
    }
}
