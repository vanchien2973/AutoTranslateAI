using Application.Interfaces;
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

    public OpenAiChatCompletionService(IOptions<OpenAIOptions> options, ExternalApiResiliencePipeline resilience)
    {
        var config = options.Value;
        _client = new ChatClient(config.Model, config.ApiKey);
        _resilience = resilience;
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

        return completion.Value.Content[0].Text;
    }
}
