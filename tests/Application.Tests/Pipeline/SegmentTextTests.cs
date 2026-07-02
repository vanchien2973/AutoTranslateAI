using Application.Pipeline;

namespace Application.Tests.Pipeline;

public class SegmentTextTests
{
    private static PipelineSegment Segment() => new()
    {
        Index = 0,
        StartTime = 0,
        EndTime = 1,
        OriginalText = "original",
    };

    [Fact]
    public void Given_OnlyOriginal_When_ForTts_Then_UsesOriginal()
    {
        // Arrange
        var segment = Segment();

        // Act / Assert
        SegmentText.ForTts(segment).Should().Be("original");
    }

    [Fact]
    public void Given_AiAndEditedAudioText_When_ForTts_Then_PrefersEdited()
    {
        // Arrange
        var segment = Segment();
        segment.AudioTextAi = "ai";
        segment.AudioTextEdited = "edited";

        // Act / Assert
        SegmentText.ForTts(segment).Should().Be("edited");
    }

    [Fact]
    public void Given_NoSubtitleText_When_ForSubtitle_Then_FallsBackToAudioThenOriginal()
    {
        // Arrange
        var segment = Segment();
        segment.AudioTextAi = "audio-ai";

        // Act / Assert
        SegmentText.ForSubtitle(segment).Should().Be("audio-ai");
    }

    [Fact]
    public void Given_EditedSubtitle_When_ForSubtitle_Then_PrefersEditedSubtitle()
    {
        // Arrange
        var segment = Segment();
        segment.AudioTextAi = "audio-ai";
        segment.SubtitleTextAi = "sub-ai";
        segment.SubtitleTextEdited = "sub-edited";

        // Act / Assert
        SegmentText.ForSubtitle(segment).Should().Be("sub-edited");
    }
}
