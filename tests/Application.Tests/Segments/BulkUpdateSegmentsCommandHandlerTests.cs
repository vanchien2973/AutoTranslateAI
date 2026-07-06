using Application.Features.Segments.BulkUpdateSegments;
using Application.Interfaces;

namespace Application.Tests.Segments;

public class BulkUpdateSegmentsCommandHandlerTests
{
    [Fact]
    public async Task Given_JobNotAwaitingReview_When_Handle_Then_ReturnsConflict()
    {
        // Arrange
        var job = TestJobs.Queued();
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var command = new BulkUpdateSegmentsCommand(job.Id, new[] { new SegmentEdit(Guid.NewGuid(), "x", null, null) });

        // Act
        var response = await new BulkUpdateSegmentsCommandHandler(jobs).Handle(command, CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Conflict);
    }

    [Fact]
    public async Task Given_UnknownSegment_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var job = TestJobs.AwaitingReview(TestJobs.Segment(0));
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var command = new BulkUpdateSegmentsCommand(job.Id, new[] { new SegmentEdit(Guid.NewGuid(), "x", null, null) });

        // Act
        var response = await new BulkUpdateSegmentsCommandHandler(jobs).Handle(command, CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.NotFound);
        await jobs.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_ValidEdits_When_Handle_Then_AppliesAllAndSaves()
    {
        // Arrange
        var first = TestJobs.Segment(0);
        var second = TestJobs.Segment(1);
        var job = TestJobs.AwaitingReview(first, second);
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var command = new BulkUpdateSegmentsCommand(job.Id, new[]
        {
            new SegmentEdit(first.Id, "a0", null, null),
            new SegmentEdit(second.Id, null, "s1", null),
        });

        // Act
        var response = await new BulkUpdateSegmentsCommandHandler(jobs).Handle(command, CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Ok);
        first.AudioTextEdited.Should().Be("a0");
        second.SubtitleTextEdited.Should().Be("s1");
        await jobs.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
