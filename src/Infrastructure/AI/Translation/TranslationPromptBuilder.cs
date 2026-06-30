using System.Text.Json;

namespace Infrastructure.AI.Translation;

internal static class TranslationPromptBuilder
{
    public static string BuildSystemPrompt(string sourceLang, string targetLang) =>
        $"You are a professional subtitle translator. Translate each string in the JSON array " +
        $"\"segments\" from {sourceLang} to {targetLang}. Preserve meaning, tone, named entities and " +
        $"numbers. Do not merge, split, reorder, add, or drop items. Respond with ONLY a JSON object " +
        $"of the form {{\"translations\": [\"...\"]}} containing exactly the same number of strings, " +
        $"in the same order as the input.";

    public static string BuildUserPrompt(IReadOnlyList<string> texts) =>
        JsonSerializer.Serialize(new { segments = texts });
}
