using Application.Interfaces;
using Infrastructure.Configuration;
using Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Infrastructure.AI.Translation;

public sealed class OpenAiTranslationService : ITranslationService
{
    private readonly ChatClient _client;
    private readonly string _model;
    private readonly ExternalApiResiliencePipeline _resilience;
    private readonly ILogger<OpenAiTranslationService> _logger;

    public OpenAiTranslationService(
        IOptions<OpenAIOptions> options,
        ExternalApiResiliencePipeline resilience,
        ILogger<OpenAiTranslationService> logger)
    {
        var config = options.Value;
        _model = config.Model;
        _client = new ChatClient(_model, config.ApiKey);
        _resilience = resilience;
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

        ChatMessage[] messages =
        [
            new SystemChatMessage(TranslationPromptBuilder.BuildSystemPrompt(sourceLang, targetLang)),
            new UserChatMessage(TranslationPromptBuilder.BuildUserPrompt(texts)),
        ];

        var chatOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
        };

        _logger.LogInformation(
            "Translating {Count} segments {Source}->{Target} with {Model}",
            texts.Count, sourceLang, targetLang, _model);

        // Retry transient failures (429/5xx/timeout) with back-off via the shared Polly pipeline.
        var completion = await _resilience.Pipeline.ExecuteAsync(
            async ct => await _client.CompleteChatAsync(messages, chatOptions, ct),
            cancellationToken);
        var content = completion.Value.Content[0].Text;

        return TranslationResponseParser.Parse(content, texts.Count);
    }
}
