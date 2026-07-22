using System.Text;
using Domain.Entities;

namespace Application.Helpers;

public static class ReviewPromptBuilder
{
    public static string BuildSystemPrompt(string? sourceLang, string audioLang, string? subtitleLang)
    {
        var source = string.IsNullOrWhiteSpace(sourceLang) ? "the source language" : sourceLang;
        var target = string.IsNullOrWhiteSpace(subtitleLang) || subtitleLang == audioLang
            ? audioLang
            : $"{audioLang} (audio) and {subtitleLang} (subtitle)";

        return
            "You are a subtitle-review assistant. The user is reviewing a translation from " +
            $"{source} to {target}. Only SUGGEST edits — never decide for the user. " +
            "Segments are numbered starting at 1; segment 1 is the first one. Use those same numbers " +
            "in \"segmentIndex\" and whenever you mention a segment in \"message\". " +
            "Respond with ONLY a JSON object matching this schema, no markdown or prose outside the JSON:\n" +
            "{\"message\": \"<short explanation for the user>\", \"proposals\": [" +
            "{\"segmentIndex\": <int, 1-based>, \"target\": \"AudioText|SubtitleText\", " +
            "\"proposedText\": \"<edited text>\", \"reason\": \"<why>\"}]}";
    }

    public static string BuildUserPrompt(
        IReadOnlyList<Segment> relevant,
        IReadOnlyList<ChatMessage> history,
        string userMessage)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Relevant segments:");
        foreach (var segment in relevant)
        {
            // +1 so the numbering matches the review table; ReviewResponseParser converts it back.
            builder.AppendLine(
                $"[{segment.SegmentIndex + 1}] source: \"{segment.OriginalText}\" | " +
                $"audio: \"{segment.TtsText}\" | subtitle: \"{segment.SubtitleText}\"");
        }

        if (history.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Conversation so far:");
            foreach (var message in history)
            {
                builder.AppendLine($"{message.Role}: {message.Content}");
            }
        }

        builder.AppendLine();
        builder.Append($"User request: {userMessage}");
        return builder.ToString();
    }
}
