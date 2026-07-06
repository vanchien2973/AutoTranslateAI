using System.Text.Json;
using Domain.Entities;

namespace Application.Helpers;

public static class ReviewResponseParser
{
    public static bool TryParse(
        string rawJson,
        IReadOnlyCollection<Segment> segments,
        out string? message,
        out IReadOnlyList<EditProposal>? proposals,
        out string? error)
    {
        message = null;
        proposals = null;
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

        var byIndex = segments.ToDictionary(segment => segment.SegmentIndex);
        var parsed = new List<EditProposal>();

        if (root.TryGetProperty("proposals", out var proposalsElement))
        {
            if (proposalsElement.ValueKind != JsonValueKind.Array)
            {
                error = "\"proposals\" was not a JSON array.";
                return false;
            }

            foreach (var item in proposalsElement.EnumerateArray())
            {
                if (!TryParseProposal(item, byIndex, out var proposal, out error))
                {
                    return false;
                }

                parsed.Add(proposal!);
            }
        }

        message = GetString(root, "message") ?? string.Empty;
        proposals = parsed;
        return true;
    }

    private static bool TryParseProposal(
        JsonElement item,
        IReadOnlyDictionary<int, Segment> byIndex,
        out EditProposal? proposal,
        out string? error)
    {
        proposal = null;
        error = null;

        if (item.ValueKind != JsonValueKind.Object
            || !item.TryGetProperty("segmentIndex", out var indexElement)
            || indexElement.ValueKind != JsonValueKind.Number
            || !indexElement.TryGetInt32(out var index))
        {
            error = "A proposal was missing a numeric \"segmentIndex\".";
            return false;
        }

        if (!byIndex.TryGetValue(index, out var segment))
        {
            error = $"Proposal referenced unknown segment index {index}.";
            return false;
        }

        if (!Enum.TryParse<EditTarget>(GetString(item, "target"), ignoreCase: true, out var target))
        {
            error = $"Proposal for segment {index} had an invalid target.";
            return false;
        }

        var proposedText = GetString(item, "proposedText");
        if (string.IsNullOrWhiteSpace(proposedText))
        {
            error = $"Proposal for segment {index} had empty proposedText.";
            return false;
        }

        var currentText = target == EditTarget.AudioText ? segment.TtsText : segment.SubtitleText;
        proposal = new EditProposal(
            ProposalId: Guid.NewGuid(),
            SegmentId: segment.Id,
            SegmentIndex: index,
            Target: target,
            CurrentText: currentText,
            ProposedText: proposedText,
            Reason: GetString(item, "reason") ?? string.Empty);
        return true;
    }

    // Reads a string property only when present and actually a JSON string; otherwise null (never throws).
    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
