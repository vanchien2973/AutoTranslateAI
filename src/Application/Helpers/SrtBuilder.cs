using System.Globalization;
using System.Text;

namespace Application.Helpers;

public static class SrtBuilder
{
    public static string Build(IReadOnlyList<PipelineSegment> segments)
    {
        var builder = new StringBuilder();
        var cueNumber = 1;

        foreach (var segment in segments.OrderBy(segment => segment.StartTime))
        {
            var text = SegmentText.ForSubtitle(segment);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            builder.Append(cueNumber++).Append('\n');
            builder.Append(FormatTimestamp(segment.StartTime))
                .Append(" --> ")
                .Append(FormatTimestamp(segment.EndTime))
                .Append('\n');
            builder.Append(text.Trim()).Append('\n').Append('\n');
        }

        return builder.ToString();
    }

    // SRT timestamp is HH:MM:SS,mmm (comma before milliseconds).
    private static string FormatTimestamp(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2},{time.Milliseconds:D3}");
    }
}
