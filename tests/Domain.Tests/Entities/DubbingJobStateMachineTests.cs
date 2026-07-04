using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Tests.Entities;

public class DubbingJobStateMachineTests
{
    private static DubbingJob NewJob() =>
        new(
            sourceUrl: "https://youtu.be/abc",
            localFilePath: null,
            sourceLanguage: "en",
            audioLanguage: "vi",
            subtitleLanguage: "vi",
            enableDubbing: true);

    // Drives a fresh job forward to the requested status through valid transitions only.
    private static DubbingJob JobIn(JobStatus status)
    {
        var job = NewJob();
        if (status == JobStatus.Queued)
        {
            return job;
        }

        job.StartDownload();
        if (status == JobStatus.DownloadingMedia)
        {
            return job;
        }

        job.StartPhase1();
        if (status == JobStatus.ProcessingPhase1)
        {
            return job;
        }

        job.MarkAwaitingReview();
        if (status == JobStatus.AwaitingReview)
        {
            return job;
        }

        job.Confirm();
        if (status == JobStatus.ConfirmedQueued)
        {
            return job;
        }

        job.StartPhase2();
        if (status == JobStatus.ProcessingPhase2)
        {
            return job;
        }

        job.StartPublishing();
        if (status == JobStatus.Publishing)
        {
            return job;
        }

        job.Complete("out.mp4");
        return job;
    }

    [Fact]
    public void Given_NewJob_When_Created_Then_StatusIsQueued()
    {
        // Act
        var job = NewJob();

        // Assert
        job.Status.Should().Be(JobStatus.Queued);
    }

    [Fact]
    public void Given_JobWithoutSourceOrFile_When_Created_Then_ThrowsBusinessRuleViolation()
    {
        // Act
        var act = () => new DubbingJob(null, null, "en", "vi", "vi", true);

        // Assert
        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Given_QueuedJob_When_StartDownload_Then_MovesToDownloadingMediaAndSetsStartedAt()
    {
        // Arrange
        var job = JobIn(JobStatus.Queued);

        // Act
        job.StartDownload();

        // Assert
        job.Status.Should().Be(JobStatus.DownloadingMedia);
        job.StartedAt.Should().NotBeNull();
        job.CurrentStep.Should().Be(StepType.Download);
    }

    [Fact]
    public void Given_ProcessingPhase1_When_MarkAwaitingReview_Then_MovesToAwaitingReviewAndSetsReviewReadyAt()
    {
        // Arrange
        var job = JobIn(JobStatus.ProcessingPhase1);

        // Act
        job.MarkAwaitingReview();

        // Assert
        job.Status.Should().Be(JobStatus.AwaitingReview);
        job.ReviewReadyAt.Should().NotBeNull();
    }

    [Fact]
    public void Given_AwaitingReview_When_Confirm_Then_MovesToConfirmedQueuedAndSetsConfirmedAt()
    {
        // Arrange
        var job = JobIn(JobStatus.AwaitingReview);

        // Act
        job.Confirm();

        // Assert
        job.Status.Should().Be(JobStatus.ConfirmedQueued);
        job.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public void Given_ProcessingPhase2_When_Complete_Then_MovesToCompletedWithOutputAndFullProgress()
    {
        // Arrange
        var job = JobIn(JobStatus.ProcessingPhase2);

        // Act
        job.Complete("https://r2/out.mp4");

        // Assert
        job.Status.Should().Be(JobStatus.Completed);
        job.OutputFilePath.Should().Be("https://r2/out.mp4");
        job.ProgressPercent.Should().Be(100);
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Given_QueuedJob_When_StartPhase2_Then_ThrowsInvalidStateTransition()
    {
        // Arrange
        var job = JobIn(JobStatus.Queued);

        // Act
        var act = () => job.StartPhase2();

        // Assert
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Given_CompletedJob_When_Cancel_Then_ThrowsInvalidStateTransition()
    {
        // Arrange
        var job = JobIn(JobStatus.Completed);

        // Act
        var act = () => job.Cancel();

        // Assert
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Given_FailedJobInPhase1_When_StartPhase1_Then_ResumesToProcessingPhase1()
    {
        // Arrange
        var job = JobIn(JobStatus.ProcessingPhase1);
        job.Fail("whisper crashed");

        // Act
        job.StartPhase1();

        // Assert
        job.Status.Should().Be(JobStatus.ProcessingPhase1);
    }

    [Fact]
    public void Given_AwaitingReview_When_Cancel_Then_MovesToCancelled()
    {
        // Arrange
        var job = JobIn(JobStatus.AwaitingReview);

        // Act
        job.Cancel();

        // Assert
        job.Status.Should().Be(JobStatus.Cancelled);
    }

    [Fact]
    public void Given_QueuedJob_When_BeginPhase1Processing_Then_MovesToProcessingPhase1()
    {
        // Arrange
        var job = JobIn(JobStatus.Queued);

        // Act
        job.BeginPhase1Processing();

        // Assert
        job.Status.Should().Be(JobStatus.ProcessingPhase1);
        job.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public void Given_FailedJob_When_BeginPhase1Processing_Then_ResumesToProcessingPhase1()
    {
        // Arrange
        var job = JobIn(JobStatus.ProcessingPhase1);
        job.Fail("step crashed");

        // Act
        job.BeginPhase1Processing();

        // Assert
        job.Status.Should().Be(JobStatus.ProcessingPhase1);
    }

    [Fact]
    public void Given_AlreadyProcessingPhase1_When_BeginPhase1Processing_Then_StaysWithoutThrowing()
    {
        // Arrange
        var job = JobIn(JobStatus.ProcessingPhase1);

        // Act
        var act = () => job.BeginPhase1Processing();

        // Assert
        act.Should().NotThrow();
        job.Status.Should().Be(JobStatus.ProcessingPhase1);
    }

    [Fact]
    public void Given_CompletedJob_When_BeginPhase1Processing_Then_ThrowsInvalidStateTransition()
    {
        // Arrange
        var job = JobIn(JobStatus.Completed);

        // Act
        var act = () => job.BeginPhase1Processing();

        // Assert
        act.Should().Throw<InvalidStateTransitionException>();
    }
}
