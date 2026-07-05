using Application.Pipeline;

namespace Application.Tests.Pipeline;

public class PipelineSegmentTests
{
    private static PipelineSegment NewSegment() => new()
    {
        Index = 0,
        StartTime = 0,
        EndTime = 2,
        OriginalText = "hello",
    };

    [Fact]
    public void Given_NoTtsClip_When_CheckNeedsTtsSynthesis_Then_True()
    {
        // Arrange
        var segment = NewSegment();

        // Act
        var needs = segment.NeedsTtsSynthesis;

        // Assert
        needs.Should().BeTrue();
    }

    [Fact]
    public void Given_TtsClipAndUnchanged_When_CheckNeedsTtsSynthesis_Then_False()
    {
        // Arrange
        var segment = NewSegment();
        segment.TtsAudioPath = "seg-0000.wav";
        segment.NeedsTtsRegenerate = false;

        // Act
        var needs = segment.NeedsTtsSynthesis;

        // Assert
        needs.Should().BeFalse();
    }

    [Fact]
    public void Given_TtsClipButAudioChanged_When_CheckNeedsTtsSynthesis_Then_True()
    {
        // Arrange
        var segment = NewSegment();
        segment.TtsAudioPath = "seg-0000.wav";
        segment.NeedsTtsRegenerate = true;

        // Act
        var needs = segment.NeedsTtsSynthesis;

        // Assert
        needs.Should().BeTrue();
    }
}
