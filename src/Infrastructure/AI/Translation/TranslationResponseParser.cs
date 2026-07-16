using System.Text.Json;

namespace Infrastructure.AI.Translation;

internal static class TranslationResponseParser
{
    public static bool TryParse(string content, int expectedCount, out IReadOnlyList<string> translations)
    {
        try
        {
            translations = Parse(content, expectedCount);
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException)
        {
            translations = [];
            return false;
        }
    }

    public static IReadOnlyList<string> Parse(string content, int expectedCount)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Translation response was empty.");
        }

        var translations = ExtractTranslations(content);
        if (translations.Count != expectedCount)
        {
            throw new InvalidOperationException(
                $"Translation count mismatch: expected {expectedCount}, got {translations.Count}.");
        }

        return translations;
    }

    private static List<string> ExtractTranslations(string content)
    {
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        JsonElement array;
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("translations", out var property))
        {
            array = property;
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            array = root; // tolerate a bare array
        }
        else
        {
            throw new InvalidOperationException("Unexpected translation response shape.");
        }

        if (array.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("\"translations\" was not a JSON array.");
        }

        var result = new List<string>(array.GetArrayLength());
        foreach (var element in array.EnumerateArray())
        {
            result.Add(element.GetString() ?? string.Empty);
        }

        return result;
    }
}
