using Application.Interfaces;
using Domain.Enums;
using Infrastructure.Configuration;
using Infrastructure.Resilience;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using ChatMessage = OpenAI.Chat.ChatMessage;

namespace Infrastructure.AI.Review;

public sealed class OpenAiChatCompletionService : ILlmCompletionService
{
    private readonly ChatClient _client;
    private readonly ExternalApiResiliencePipeline _resilience;
    private readonly IUsageTracker _usage;

    public OpenAiChatCompletionService(
        IOptions<OpenAIOptions> options,
        ExternalApiResiliencePipeline resilience,
        IUsageTracker usage)
    {
        var config = options.Value;
        _client = new ChatClient(config.Model, config.ApiKey);
        _resilience = resilience;
        _usage = usage;
    }

    public async Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        ChatMessage[] messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt),
        ];

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
        };

        var completion = await _resilience.Pipeline.ExecuteAsync(
            async ct => await _client.CompleteChatAsync(messages, options, ct),
            cancellationToken);

        var tokens = completion.Value.Usage;
        await _usage.RecordAsync(
            new UsageEntry("OpenAI", "LlmCompletion", UsageUnit.Tokens, tokens.InputTokenCount, tokens.OutputTokenCount),
            cancellationToken);

        return completion.Value.Content[0].Text;
    }
}
