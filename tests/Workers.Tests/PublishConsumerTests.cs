using Application.Interfaces;
using Application.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Workers.Consumers;

namespace Workers.Tests;

public class PublishConsumerTests
{
    private static readonly PublishTarget YouTubeTarget = new(PublishPlatform.YouTube, null, "Title", null, null);

    private readonly IPublishStep _step = Substitute.For<IPublishStep>();
    private readonly IDubbingJobRepository _jobs = Substitute.For<IDubbingJobRepository>();
    private readonly IProgressNotifier _progress = Substitute.For<IProgressNotifier>();

    private PublishConsumer BuildConsumer() =>
        new(_step, _jobs, _progress, NullLogger<PublishConsumer>.Instance);

    private static ConsumeContext<DubbingJobPublishRequested> Context(Guid jobId)
    {
        var context = Substitute.For<ConsumeContext<DubbingJobPublishRequested>>();
        context.Message.Returns(new DubbingJobPublishRequested(jobId, [YouTubeTarget]));
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Given_MissingJob_When_Consume_Then_DoesNothing()
    {
        var jobId = Guid.NewGuid();
        _jobs.GetAsync(jobId, Arg.Any<CancellationToken>()).Returns((DubbingJob?)null);

        await BuildConsumer().Consume(Context(jobId));

        await _step.DidNotReceive().ExecuteAsync(Arg.Any<DubbingJob>(), Arg.Any<IReadOnlyList<PublishTarget>>(), Arg.Any<CancellationToken>());
        await _jobs.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_JobNotCompleted_When_Consume_Then_SkipsWithoutPublishing()
    {
        var job = TestJobs.AwaitingReview();
        _jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        await BuildConsumer().Consume(Context(job.Id));

        job.Status.Should().Be(JobStatus.AwaitingReview);
        await _step.DidNotReceive().ExecuteAsync(Arg.Any<DubbingJob>(), Arg.Any<IReadOnlyList<PublishTarget>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_CompletedJob_When_Consume_Then_PublishesInPublishingStateThenBackToCompleted()
    {
        var job = TestJobs.Completed();
        _jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        _step.ExecuteAsync(job, Arg.Any<IReadOnlyList<PublishTarget>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                job.Status.Should().Be(JobStatus.Publishing);
                return (IReadOnlyList<PublishTargetResult>)Array.Empty<PublishTargetResult>();
            });

        await BuildConsumer().Consume(Context(job.Id));

        job.Status.Should().Be(JobStatus.Completed);
        job.OutputFilePath.Should().Be("out.mp4");
        await _step.Received(1).ExecuteAsync(job, Arg.Any<IReadOnlyList<PublishTarget>>(), Arg.Any<CancellationToken>());
        await _progress.Received().ReportAsync(Arg.Is<JobProgress>(p => p.Status == "Publishing"), Arg.Any<CancellationToken>());
        await _progress.Received().ReportAsync(Arg.Is<JobProgress>(p => p.Status == "Completed"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_PublishStepThrows_When_Consume_Then_RevertsToCompleted_AndRethrows()
    {
        var job = TestJobs.Completed();
        _jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _step.ExecuteAsync(job, Arg.Any<IReadOnlyList<PublishTarget>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<PublishTargetResult>>(_ => throw new InvalidOperationException("broker down"));

        var act = () => BuildConsumer().Consume(Context(job.Id));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("broker down");
        job.Status.Should().Be(JobStatus.Completed);
    }
}
