using System.Text.Json;

namespace Application.Helpers;

public static class SeoResponseParser
{
    public static bool TryParse(string rawJson, out SeoMetadata? metadata, out string? error)
    {
        metadata = null;
        error = null;

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            error = "LLM response was empty.";
            return false;
        }

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(rawJson);
            root = document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            error = $"LLM response was not valid JSON: {exception.Message}";
            return false;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            error = "LLM response was not a JSON object.";
            return false;
        }

        var title = GetString(root, "title");
        if (string.IsNullOrWhiteSpace(title))
        {
            error = "LLM response was missing a title.";
            return false;
        }

        var tags = new List<string>();
        if (root.TryGetProperty("tags", out var tagsElement) && tagsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var tag in tagsElement.EnumerateArray())
            {
                if (tag.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(tag.GetString()))
                {
                    tags.Add(tag.GetString()!.Trim());
                }
            }
        }

        metadata = new SeoMetadata(title!.Trim(), GetString(root, "description")?.Trim() ?? string.Empty, tags);
        return true;
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
