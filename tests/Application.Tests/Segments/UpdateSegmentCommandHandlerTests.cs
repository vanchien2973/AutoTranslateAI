using Application.Features.Segments.UpdateSegment;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Tests.Segments;

public class UpdateSegmentCommandHandlerTests
{
    [Fact]
    public async Task Given_MissingJob_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DubbingJob?)null);

        // Act
        var response = await new UpdateSegmentCommandHandler(jobs)
            .Handle(new UpdateSegmentCommand(Guid.NewGuid(), Guid.NewGuid(), "x", null, null), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.NotFound);
    }

    [Fact]
    public async Task Given_JobNotAwaitingReview_When_Handle_Then_ReturnsConflict()
    {
        // Arrange
        var job = TestJobs.Queued();
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        var response = await new UpdateSegmentCommandHandler(jobs)
            .Handle(new UpdateSegmentCommand(job.Id, Guid.NewGuid(), "x", null, null), CancellationToken.None);

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

        // Act
        var response = await new UpdateSegmentCommandHandler(jobs)
            .Handle(new UpdateSegmentCommand(job.Id, Guid.NewGuid(), "x", null, null), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.NotFound);
    }

    [Fact]
    public async Task Given_ValidEdit_When_Handle_Then_AppliesAndSaves()
    {
        // Arrange
        var segment = TestJobs.Segment(0);
        var job = TestJobs.AwaitingReview(segment);
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        // Act
        var response = await new UpdateSegmentCommandHandler(jobs)
            .Handle(new UpdateSegmentCommand(job.Id, segment.Id, "changed", null, null), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Ok);
        response.Segment!.AudioTextEdited.Should().Be("changed");
        segment.IsEdited.Should().BeTrue();
        await jobs.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
