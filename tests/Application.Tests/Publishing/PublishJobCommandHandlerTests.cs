using Application.Features.Publishing.PublishJob;
using Application.Interfaces;
using Application.Messaging;
using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Publishing;

public class PublishJobCommandHandlerTests
{
    private static IDubbingJobRepository JobsReturning(DubbingJob? job)
    {
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(job);
        return jobs;
    }

    private static PublishTarget YouTubeTarget => new(PublishPlatform.YouTube, null, "Title", null, null);

    [Fact]
    public async Task Given_MissingJob_When_Handle_Then_JobNotFound_AndNothingEnqueued()
    {
        var events = Substitute.For<IEventPublisher>();
        var handler = new PublishJobCommandHandler(JobsReturning(null), events);

        var response = await handler.Handle(new PublishJobCommand(Guid.NewGuid(), [YouTubeTarget]), CancellationToken.None);

        response.Status.Should().Be(PublishJobStatus.JobNotFound);
        await events.DidNotReceive().PublishAsync(Arg.Any<DubbingJobPublishRequested>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_JobNotCompleted_When_Handle_Then_NotCompleted()
    {
        var events = Substitute.For<IEventPublisher>();
        var handler = new PublishJobCommandHandler(JobsReturning(TestJobs.AwaitingReview(TestJobs.Segment(0))), events);

        var response = await handler.Handle(new PublishJobCommand(Guid.NewGuid(), [YouTubeTarget]), CancellationToken.None);

        response.Status.Should().Be(PublishJobStatus.NotCompleted);
        await events.DidNotReceive().PublishAsync(Arg.Any<DubbingJobPublishRequested>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_CompletedJobAndTargets_When_Handle_Then_EnqueuesWithTargets()
    {
        var events = Substitute.For<IEventPublisher>();
        var job = TestJobs.Completed();
        var handler = new PublishJobCommandHandler(JobsReturning(job), events);

        var response = await handler.Handle(new PublishJobCommand(job.Id, [YouTubeTarget]), CancellationToken.None);

        response.Status.Should().Be(PublishJobStatus.Ok);
        await events.Received(1).PublishAsync(
            Arg.Is<DubbingJobPublishRequested>(m => m.JobId == job.Id && m.Targets.Count == 1),
            Arg.Any<CancellationToken>());
    }
}
