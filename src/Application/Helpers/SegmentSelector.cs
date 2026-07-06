using System.Text.RegularExpressions;
using Domain.Entities;

namespace Application.Helpers;

public static class SegmentSelector
{
    private static readonly Regex NumberPattern = new(@"\d+", RegexOptions.Compiled);

    public static IReadOnlyList<Segment> Pick(IReadOnlyCollection<Segment> segments, string userMessage)
    {
        var ordered = segments.OrderBy(segment => segment.SegmentIndex).ToList();
        if (ordered.Count == 0)
        {
            return ordered;
        }

        var referenced = NumberPattern.Matches(userMessage ?? string.Empty)
            .Where(match => int.TryParse(match.Value, out _))
            .Select(match => int.Parse(match.Value))
            .ToHashSet();

        if (referenced.Count == 0)
        {
            return ordered;
        }

        var wanted = new HashSet<int>();
        foreach (var index in referenced)
        {
            wanted.Add(index - 1);
            wanted.Add(index);
            wanted.Add(index + 1);
        }

        var picked = ordered.Where(segment => wanted.Contains(segment.SegmentIndex)).ToList();

        return picked.Count > 0 ? picked : ordered;
    }
}
