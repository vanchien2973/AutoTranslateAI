using Application.Features.Segments.AdjustSegmentTiming;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Tests.Segments;

public class AdjustSegmentTimingCommandHandlerTests
{
    // Three segments with gaps: [0,3] . [4,6] . [8,10]
    private static (DubbingJob Job, Segment[] Segments) AwaitingReviewWithSegments()
    {
        var segments = new[]
        {
            new Segment(Guid.NewGuid(), 0, 0, 3, "a"),
            new Segment(Guid.NewGuid(), 1, 4, 6, "b"),
            new Segment(Guid.NewGuid(), 2, 8, 10, "c"),
        };
        return (TestJobs.AwaitingReview(segments), segments);
    }

    private static IDubbingJobRepository RepoWith(DubbingJob? job)
    {
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job?.Id ?? Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(job);
        return jobs;
    }

    [Fact]
    public async Task Given_MissingJob_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DubbingJob?)null);

        // Act
        var response = await new AdjustSegmentTimingCommandHandler(jobs)
            .Handle(new AdjustSegmentTimingCommand(Guid.NewGuid(), Guid.NewGuid(), 1, 2), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.NotFound);
    }

    [Fact]
    public async Task Given_JobNotAwaitingReview_When_Handle_Then_ReturnsConflict()
    {
        // Arrange
        var job = TestJobs.Queued();
        var jobs = RepoWith(job);

        // Act
        var response = await new AdjustSegmentTimingCommandHandler(jobs)
            .Handle(new AdjustSegmentTimingCommand(job.Id, Guid.NewGuid(), 1, 2), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Conflict);
    }

    [Fact]
    public async Task Given_UnknownSegment_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var (job, _) = AwaitingReviewWithSegments();
        var jobs = RepoWith(job);

        // Act
        var response = await new AdjustSegmentTimingCommandHandler(jobs)
            .Handle(new AdjustSegmentTimingCommand(job.Id, Guid.NewGuid(), 3, 8), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.NotFound);
    }

    [Fact]
    public async Task Given_OverlappingTiming_When_Handle_Then_ReturnsConflictAndDoesNotSave()
    {
        // Arrange
        var (job, segments) = AwaitingReviewWithSegments();
        var jobs = RepoWith(job);

        // Act — end 9 overlaps the next segment (starts at 8)
        var response = await new AdjustSegmentTimingCommandHandler(jobs)
            .Handle(new AdjustSegmentTimingCommand(job.Id, segments[1].Id, 4, 9), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Conflict);
        await jobs.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_ValidTiming_When_Handle_Then_UpdatesAndSaves()
    {
        // Arrange
        var (job, segments) = AwaitingReviewWithSegments();
        var jobs = RepoWith(job);

        // Act — borrow both gaps: [3,8]
        var response = await new AdjustSegmentTimingCommandHandler(jobs)
            .Handle(new AdjustSegmentTimingCommand(job.Id, segments[1].Id, 3, 8), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Ok);
        response.Segment!.StartTime.Should().Be(3);
        response.Segment.EndTime.Should().Be(8);
        await jobs.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
