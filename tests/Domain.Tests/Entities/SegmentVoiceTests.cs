using Domain.Entities;

namespace Domain.Tests.Entities;

public class SegmentVoiceTests
{
    private static Segment WithClip()
    {
        var segment = new Segment(Guid.NewGuid(), 0, 0, 2, "hello");
        segment.AssignVoice(speakerLabel: "A", voice: "vi-VN-HoaiMyNeural");
        segment.SetTtsResult("seg0.wav", 1800, "vi-VN-HoaiMyNeural"); // clip exists, regenerate flag cleared
        return segment;
    }

    [Fact]
    public void Given_ExistingClip_When_AssignDifferentVoice_Then_FlagsRegenerate()
    {
        // Arrange
        var segment = WithClip();

        // Act
        segment.AssignVoice("A", "vi-VN-NamMinhNeural");

        // Assert
        segment.NeedsTtsRegenerate.Should().BeTrue();
        segment.AssignedVoice.Should().Be("vi-VN-NamMinhNeural");
    }

    [Fact]
    public void Given_ExistingClip_When_ChangeOnlySpeakerLabel_Then_DoesNotFlagRegenerate()
    {
        // Arrange
        var segment = WithClip();

        // Act — same voice, different label
        segment.AssignVoice("Narrator", "vi-VN-HoaiMyNeural");

        // Assert
        segment.NeedsTtsRegenerate.Should().BeFalse();
        segment.SpeakerLabel.Should().Be("Narrator");
    }

    [Fact]
    public void Given_NoClipYet_When_AssignVoice_Then_DoesNotFlagRegenerate()
    {
        // Arrange — first pass, no clip synthesized yet
        var segment = new Segment(Guid.NewGuid(), 0, 0, 2, "hello");

        // Act
        segment.AssignVoice("A", "vi-VN-NamMinhNeural");

        // Assert — nothing to regenerate; the first TTS pass will use the assigned voice anyway
        segment.NeedsTtsRegenerate.Should().BeFalse();
    }
}
