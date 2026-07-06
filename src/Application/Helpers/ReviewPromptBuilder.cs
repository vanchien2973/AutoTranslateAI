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
            "Respond with ONLY a JSON object matching this schema, no markdown or prose outside the JSON:\n" +
            "{\"message\": \"<short explanation for the user>\", \"proposals\": [" +
            "{\"segmentIndex\": <int>, \"target\": \"AudioText|SubtitleText\", " +
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
            builder.AppendLine(
                $"[{segment.SegmentIndex}] source: \"{segment.OriginalText}\" | " +
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
