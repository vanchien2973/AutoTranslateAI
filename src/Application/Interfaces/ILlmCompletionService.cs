namespace Application.Interfaces;

public interface ILlmCompletionService
{
    Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
}
