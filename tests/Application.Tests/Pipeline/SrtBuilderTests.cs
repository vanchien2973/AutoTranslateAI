using System.Text.RegularExpressions;

namespace Application.Tests.Pipeline;

public class SrtBuilderTests
{
    [Fact]
    public void Given_Segments_When_Build_Then_ProducesNumberedCuesWithSrtTimestamps()
    {
        // Arrange
        var segments = new List<PipelineSegment>
        {
            new() { Index = 0, StartTime = 0, EndTime = 1.5, OriginalText = "hello", SubtitleTextAi = "xin chào" },
            new() { Index = 1, StartTime = 1.5, EndTime = 3, OriginalText = "world", SubtitleTextAi = "thế giới" },
        };

        // Act
        var srt = SrtBuilder.Build(segments);

        // Assert
        srt.Should().Contain("1\n00:00:00,000 --> 00:00:01,500\nxin chào");
        srt.Should().Contain("2\n00:00:01,500 --> 00:00:03,000\nthế giới");
    }

    [Fact]
    public void Given_HoursAndMillis_When_Build_Then_FormatsTimestampCorrectly()
    {
        // Arrange
        var segments = new List<PipelineSegment>
        {
            new() { Index = 0, StartTime = 3661.25, EndTime = 3662, OriginalText = "x" },
        };

        // Act / Assert — 1h 1m 1.25s -> 1h 1m 2s
        SrtBuilder.Build(segments).Should().Contain("01:01:01,250 --> 01:01:02,000");
    }

    [Fact]
    public void Given_OnlyOriginalText_When_Build_Then_UsesOriginalViaCoalesce()
    {
        // Arrange
        var segments = new List<PipelineSegment>
        {
            new() { Index = 0, StartTime = 0, EndTime = 1, OriginalText = "only original" },
        };

        // Act / Assert
        SrtBuilder.Build(segments).Should().Contain("only original");
    }

    [Fact]
    public void Given_UnorderedSegments_When_Build_Then_OrdersByStartTime()
    {
        // Arrange
        var segments = new List<PipelineSegment>
        {
            new() { Index = 1, StartTime = 2, EndTime = 3, OriginalText = "second" },
            new() { Index = 0, StartTime = 0, EndTime = 1, OriginalText = "first" },
        };

        // Act
        var srt = SrtBuilder.Build(segments);

        // Assert
        srt.IndexOf("first", StringComparison.Ordinal).Should().BeLessThan(srt.IndexOf("second", StringComparison.Ordinal));
    }

    [Fact]
    public void Given_EmptyTextSegment_When_Build_Then_SkipsItAndRenumbers()
    {
        // Arrange
        var segments = new List<PipelineSegment>
        {
            new() { Index = 0, StartTime = 0, EndTime = 1, OriginalText = "" },      // skipped
            new() { Index = 1, StartTime = 1, EndTime = 2, OriginalText = "kept" },
        };

        // Act
        var srt = SrtBuilder.Build(segments);

        // Assert
        Regex.Matches(srt, "-->").Count.Should().Be(1);
        srt.Should().Contain("1\n00:00:01,000 --> 00:00:02,000\nkept");
    }
}
