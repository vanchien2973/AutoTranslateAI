using Application.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Workers.Steps;

namespace Workers.Tests;

public class PublishStepTests
{
    private static readonly PublishTarget YouTubeTarget = new(PublishPlatform.YouTube, null, "Title", null, null);

    [Fact]
    public async Task Given_ExecutorSucceeds_When_Execute_Then_TracksStartThenComplete_AndReturnsResults()
    {
        // Arrange
        var job = TestJobs.Completed();
        var expected = new List<PublishTargetResult>
        {
            new(PublishPlatform.YouTube, Guid.NewGuid(), PublishStatus.Published, "ext", "https://youtu.be/ext", null),
        };
        var executor = Substitute.For<IPublishExecutor>();
        executor.ExecuteAsync(job, Arg.Any<IReadOnlyList<PublishTarget>>(), Arg.Any<CancellationToken>())
            .Returns(expected);
        var tracker = Substitute.For<IJobStepTracker>();
        var step = new PublishStep(executor, tracker, NullLogger<PublishStep>.Instance);

        // Act
        var results = await step.ExecuteAsync(job, [YouTubeTarget], CancellationToken.None);

        // Assert
        results.Should().BeEquivalentTo(expected);
        await tracker.Received(1).StartAsync(job.Id, StepType.Publish, Arg.Any<CancellationToken>());
        await tracker.Received(1).CompleteAsync(job.Id, StepType.Publish, null, Arg.Any<CancellationToken>());
        await tracker.DidNotReceive().FailAsync(job.Id, StepType.Publish, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_ExecutorThrows_When_Execute_Then_MarksStepFailed_AndRethrows()
    {
        // Arrange
        var job = TestJobs.Completed();
        var executor = Substitute.For<IPublishExecutor>();
        executor.ExecuteAsync(job, Arg.Any<IReadOnlyList<PublishTarget>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<PublishTargetResult>>(_ => throw new InvalidOperationException("db down"));
        var tracker = Substitute.For<IJobStepTracker>();
        var step = new PublishStep(executor, tracker, NullLogger<PublishStep>.Instance);

        // Act
        var act = () => step.ExecuteAsync(job, [YouTubeTarget], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
        await tracker.Received(1).StartAsync(job.Id, StepType.Publish, Arg.Any<CancellationToken>());
        await tracker.Received(1).FailAsync(job.Id, StepType.Publish, "db down", Arg.Any<CancellationToken>());
        await tracker.DidNotReceive().CompleteAsync(job.Id, StepType.Publish, Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
