using Domain.Entities;
using Domain.Exceptions;

namespace Domain.Tests.Entities;

public class SegmentTimingTests
{
    private static Segment NewSegment() => new(Guid.NewGuid(), 0, 0, 3, "text");

    [Fact]
    public void Given_NegativeStart_When_AdjustTiming_Then_Throws()
    {
        // Arrange
        var segment = NewSegment();

        // Act
        var act = () => segment.AdjustTiming(-1, 3);

        // Assert
        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Given_EndNotAfterStart_When_AdjustTiming_Then_Throws()
    {
        // Arrange
        var segment = NewSegment();

        // Act / Assert
        ((Action)(() => segment.AdjustTiming(3, 3))).Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Given_ValidTimes_When_AdjustTiming_Then_UpdatesWindow()
    {
        // Arrange
        var segment = NewSegment();

        // Act
        segment.AdjustTiming(1, 4);

        // Assert
        segment.StartTime.Should().Be(1);
        segment.EndTime.Should().Be(4);
    }

    [Fact]
    public void Given_ExistingTtsClip_When_AdjustTiming_Then_FlagsRegenerate()
    {
        // Arrange
        var segment = NewSegment();
        segment.SetTtsResult("seg.wav", 2000);

        // Act
        segment.AdjustTiming(0, 4);

        // Assert
        segment.NeedsTtsRegenerate.Should().BeTrue();
    }

    [Fact]
    public void Given_NoTtsClip_When_AdjustTiming_Then_DoesNotFlagRegenerate()
    {
        // Arrange
        var segment = NewSegment();

        // Act
        segment.AdjustTiming(0, 4);

        // Assert
        segment.NeedsTtsRegenerate.Should().BeFalse();
    }
}
