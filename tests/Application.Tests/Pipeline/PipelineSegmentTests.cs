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

    [Fact]
    public void Given_TtsClipButAssignedVoiceDiffersFromClipVoice_When_CheckNeedsTtsSynthesis_Then_True()
    {
        // Arrange — clip was made with the male voice, user now wants a female voice
        var segment = NewSegment();
        segment.TtsAudioPath = "seg-0000.wav";
        segment.TtsVoice = "vi-VN-NamMinhNeural";
        segment.AssignedVoice = "vi-VN-HoaiMyNeural";
        segment.NeedsTtsRegenerate = false;

        // Act / Assert
        segment.NeedsTtsSynthesis.Should().BeTrue();
    }

    [Fact]
    public void Given_TtsClipAndAssignedVoiceMatchesClipVoice_When_CheckNeedsTtsSynthesis_Then_False()
    {
        // Arrange
        var segment = NewSegment();
        segment.TtsAudioPath = "seg-0000.wav";
        segment.TtsVoice = "vi-VN-HoaiMyNeural";
        segment.AssignedVoice = "vi-VN-HoaiMyNeural";
        segment.NeedsTtsRegenerate = false;

        // Act / Assert
        segment.NeedsTtsSynthesis.Should().BeFalse();
    }

    [Fact]
    public void Given_AutoVoiceWithClip_When_CheckNeedsTtsSynthesis_Then_False()
    {
        // Arrange — no explicit voice (auto): reuse the clip regardless of its recorded voice
        var segment = NewSegment();
        segment.TtsAudioPath = "seg-0000.wav";
        segment.TtsVoice = "vi-VN-NamMinhNeural";
        segment.NeedsTtsRegenerate = false;

        // Act / Assert
        segment.NeedsTtsSynthesis.Should().BeFalse();
    }
}
