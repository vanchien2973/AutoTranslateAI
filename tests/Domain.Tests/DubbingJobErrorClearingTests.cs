using Domain.Entities;
using Domain.Enums;
namespace Domain.Tests;

public class DubbingJobErrorClearingTests
{
    private static DubbingJob NewJob() =>
        new(sourceUrl: "https://example.com/v", localFilePath: null, sourceLanguage: null,
            audioLanguage: "vi", subtitleLanguage: "vi", enableDubbing: true);

    [Fact]
    public void Given_FailedJob_When_RetriedInPhase1_Then_ErrorCleared()
    {
        var job = NewJob();
        job.BeginPhase1Processing();
        job.Fail("download timed out");

        job.BeginPhase1Processing(); // broker redelivers the Phase 1 message

        job.Status.Should().Be(JobStatus.ProcessingPhase1);
        job.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Given_FailedPhase2_When_RetriedInPhase2_Then_ErrorCleared()
    {
        var job = NewJob();
        job.BeginPhase1Processing();
        job.MarkAwaitingReview();
        job.Confirm();
        job.BeginPhase2Processing();
        job.Fail("render crashed");

        job.BeginPhase2Processing();

        job.Status.Should().Be(JobStatus.ProcessingPhase2);
        job.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Given_FailedThenRetried_When_Completed_Then_NoLingeringError()
    {
        var job = NewJob();
        job.BeginPhase1Processing();
        job.MarkAwaitingReview();
        job.Confirm();
        job.BeginPhase2Processing();
        job.Fail("transient upload error");
        job.BeginPhase2Processing();

        job.Complete("s3://output.mp4");

        job.Status.Should().Be(JobStatus.Completed);
        job.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Given_FailedJob_Then_ErrorIsStillReadableWhileFailed()
    {
        var job = NewJob();
        job.BeginPhase1Processing();

        job.Fail("no audio track found");

        // Only cleared on the way out of Failed — while Failed, the reason must still be visible.
        job.ErrorMessage.Should().Be("no audio track found");
    }
}
