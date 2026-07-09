using Domain.Entities;

namespace Domain.Tests.Entities;

public class SegmentTextResolutionTests
{
    private static Segment NewSegment() =>
        new(jobId: Guid.NewGuid(), segmentIndex: 0, startTime: 0, endTime: 2, originalText: "hello");

    [Fact]
    public void Given_OnlyOriginalText_When_ResolveTtsText_Then_UsesOriginal()
    {
        // Arrange
        var segment = NewSegment();

        // Act
        var text = segment.TtsText;

        // Assert
        text.Should().Be("hello");
    }

    [Fact]
    public void Given_AiButNoEdit_When_ResolveTtsText_Then_UsesAiTranslation()
    {
        // Arrange
        var segment = NewSegment();
        segment.SetAiTranslation(audioTextAi: "xin chào", subtitleTextAi: null);

        // Act
        var text = segment.TtsText;

        // Assert
        text.Should().Be("xin chào");
    }

    [Fact]
    public void Given_UserEditedAudio_When_ResolveTtsText_Then_EditedWinsOverAi()
    {
        // Arrange
        var segment = NewSegment();
        segment.SetAiTranslation(audioTextAi: "xin chào", subtitleTextAi: null);
        segment.EditAudioText("chào bạn");

        // Act
        var text = segment.TtsText;

        // Assert
        text.Should().Be("chào bạn");
    }

    [Fact]
    public void Given_NoSubtitleText_When_ResolveSubtitleText_Then_FallsBackToAudioThenOriginal()
    {
        // Arrange
        var segment = NewSegment();
        segment.SetAiTranslation(audioTextAi: "xin chào", subtitleTextAi: null);

        // Act
        var text = segment.SubtitleText;

        // Assert
        text.Should().Be("xin chào");
    }

    [Fact]
    public void Given_EditAudioTextAfterTtsGenerated_When_Edit_Then_FlagsNeedsTtsRegenerate()
    {
        // Arrange
        var segment = NewSegment();
        segment.SetTtsResult(ttsAudioPath: "seg0.wav", ttsDurationMs: 1800, voice: null);

        // Act
        segment.EditAudioText("bản mới");

        // Assert
        segment.NeedsTtsRegenerate.Should().BeTrue();
    }

    [Fact]
    public void Given_EditAudioTextBeforeTts_When_Edit_Then_DoesNotFlagRegenerate()
    {
        // Arrange
        var segment = NewSegment();

        // Act
        segment.EditAudioText("bản mới");

        // Assert
        segment.NeedsTtsRegenerate.Should().BeFalse();
    }

    [Fact]
    public void Given_RegenerateFlagged_When_SetTtsResult_Then_ClearsFlag()
    {
        // Arrange
        var segment = NewSegment();
        segment.SetTtsResult("seg0.wav", 1800, null);
        segment.EditAudioText("bản mới");

        // Act
        segment.SetTtsResult("seg0-v2.wav", 2000, null);

        // Assert
        segment.NeedsTtsRegenerate.Should().BeFalse();
    }
}
