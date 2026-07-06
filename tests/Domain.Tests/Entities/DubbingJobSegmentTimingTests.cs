using Domain.Entities;
using Domain.Exceptions;

namespace Domain.Tests.Entities;

public class DubbingJobSegmentTimingTests
{
    // Three segments with gaps: [0,3] . [4,6] . [8,10]
    private static DubbingJob JobWithSegments(out Segment[] segments)
    {
        var job = new DubbingJob("https://youtu.be/x", null, "en", "vi", "vi", true);
        segments =
        [
            new Segment(Guid.NewGuid(), 0, 0, 3, "a"),
            new Segment(Guid.NewGuid(), 1, 4, 6, "b"),
            new Segment(Guid.NewGuid(), 2, 8, 10, "c"),
        ];
        job.SetSegments(segments);
        return job;
    }

    [Fact]
    public void Given_StartOverlapsPrevious_When_AdjustSegmentTiming_Then_Throws()
    {
        // Arrange
        var job = JobWithSegments(out var segments);

        // Act — start 2 is before the previous segment's end (3)
        var act = () => job.AdjustSegmentTiming(segments[1].Id, 2, 6);

        // Assert
        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Given_EndOverlapsNext_When_AdjustSegmentTiming_Then_Throws()
    {
        // Arrange
        var job = JobWithSegments(out var segments);

        // Act — end 9 is past the next segment's start (8)
        var act = () => job.AdjustSegmentTiming(segments[1].Id, 4, 9);

        // Assert
        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Given_WithinNeighbourGaps_When_AdjustSegmentTiming_Then_Updates()
    {
        // Arrange
        var job = JobWithSegments(out var segments);

        // Act — borrow both surrounding gaps: [3,8]
        job.AdjustSegmentTiming(segments[1].Id, 3, 8);

        // Assert
        segments[1].StartTime.Should().Be(3);
        segments[1].EndTime.Should().Be(8);
    }

    [Fact]
    public void Given_UnknownSegment_When_AdjustSegmentTiming_Then_Throws()
    {
        // Arrange
        var job = JobWithSegments(out _);

        // Act / Assert
        ((Action)(() => job.AdjustSegmentTiming(Guid.NewGuid(), 1, 2))).Should().Throw<BusinessRuleViolationException>();
    }
}
