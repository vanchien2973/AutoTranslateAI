using Domain.Entities;

namespace Application.Helpers;

public static class SeoPromptBuilder
{
    private const int MaxTranscriptChars = 2000; // keep token cost low; the model doesn't need the whole transcript

    public static string BuildSystemPrompt(string language) =>
        "You are a YouTube/Facebook SEO assistant. From the video transcript, write an engaging title, a " +
        $"description, and relevant tags in {language}. Respond with ONLY a JSON object, no markdown:\n" +
        "{\"title\": \"<= 100 chars\", \"description\": \"<= 500 chars\", \"tags\": [\"tag1\", \"tag2\"]}";

    public static string BuildUserPrompt(IReadOnlyCollection<Segment> segments)
    {
        var transcript = string.Join(" ", segments.OrderBy(segment => segment.SegmentIndex).Select(segment => segment.TtsText));
        if (transcript.Length > MaxTranscriptChars)
        {
            transcript = transcript[..MaxTranscriptChars];
        }

        return $"Transcript:\n{transcript}";
    }
}
