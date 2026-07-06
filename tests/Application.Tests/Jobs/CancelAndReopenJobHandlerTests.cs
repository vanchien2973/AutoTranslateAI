using Application.Features.Jobs.CancelJob;
using Application.Features.Jobs.ReopenJob;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Jobs;

public class CancelAndReopenJobHandlerTests
{
    private static IDubbingJobRepository RepoWith(DubbingJob? job, Guid id)
    {
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(id, Arg.Any<CancellationToken>()).Returns(job);
        return jobs;
    }

    [Fact]
    public async Task Given_QueuedJob_When_Cancel_Then_MovesToCancelled()
    {
        // Arrange
        var job = TestJobs.Queued();
        var handler = new CancelJobCommandHandler(RepoWith(job, job.Id));

        // Act
        var response = await handler.Handle(new CancelJobCommand(job.Id), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Ok);
        job.Status.Should().Be(JobStatus.Cancelled);
    }

    [Fact]
    public async Task Given_CancelledJob_When_Cancel_Then_ReturnsConflict()
    {
        // Arrange
        var job = TestJobs.Cancelled();
        var handler = new CancelJobCommandHandler(RepoWith(job, job.Id));

        // Act
        var response = await handler.Handle(new CancelJobCommand(job.Id), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Conflict);
    }

    [Fact]
    public async Task Given_MissingJob_When_Cancel_Then_ReturnsNotFound()
    {
        // Arrange
        var handler = new CancelJobCommandHandler(RepoWith(null, Guid.Empty));

        // Act
        var response = await handler.Handle(new CancelJobCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.NotFound);
    }

    [Fact]
    public async Task Given_CompletedJob_When_Reopen_Then_MovesToAwaitingReview()
    {
        // Arrange
        var job = TestJobs.Completed();
        var handler = new ReopenJobCommandHandler(RepoWith(job, job.Id));

        // Act
        var response = await handler.Handle(new ReopenJobCommand(job.Id), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Ok);
        job.Status.Should().Be(JobStatus.AwaitingReview);
    }

    [Fact]
    public async Task Given_QueuedJob_When_Reopen_Then_ReturnsConflict()
    {
        // Arrange
        var job = TestJobs.Queued();
        var handler = new ReopenJobCommandHandler(RepoWith(job, job.Id));

        // Act
        var response = await handler.Handle(new ReopenJobCommand(job.Id), CancellationToken.None);

        // Assert
        response.Status.Should().Be(OperationStatus.Conflict);
    }
}
