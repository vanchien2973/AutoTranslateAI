using Application.Features.Jobs.ConfirmJob;
using Application.Interfaces;
using Application.Messaging;
using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Jobs;

public class ConfirmJobCommandHandlerTests
{
    [Fact]
    public async Task Given_AwaitingReviewJob_When_Handle_Then_ConfirmsAndPublishes()
    {
        // Arrange
        var job = TestJobs.AwaitingReview(TestJobs.Segment(0));
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var events = Substitute.For<IEventPublisher>();
        var handler = new ConfirmJobCommandHandler(jobs, events);

        // Act
        var response = await handler.Handle(new ConfirmJobCommand(job.Id), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Ok);
        job.Status.Should().Be(JobStatus.ConfirmedQueued);
        await events.Received(1).PublishAsync(Arg.Is<DubbingJobConfirmed>(m => m.JobId == job.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_MissingJob_When_Handle_Then_ReturnsNotFound()
    {
        // Arrange
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DubbingJob?)null);
        var handler = new ConfirmJobCommandHandler(jobs, Substitute.For<IEventPublisher>());

        // Act
        var response = await handler.Handle(new ConfirmJobCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.NotFound);
    }

    [Fact]
    public async Task Given_InvalidState_When_Handle_Then_ReturnsConflictAndDoesNotPublish()
    {
        // Arrange
        var job = TestJobs.Queued();
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var events = Substitute.For<IEventPublisher>();
        var handler = new ConfirmJobCommandHandler(jobs, events);

        // Act
        var response = await handler.Handle(new ConfirmJobCommand(job.Id), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Conflict);
        await events.DidNotReceive().PublishAsync(Arg.Any<DubbingJobConfirmed>(), Arg.Any<CancellationToken>());
    }
}
