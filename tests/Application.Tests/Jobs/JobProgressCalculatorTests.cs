using Application.Helpers;
using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Jobs;

public class JobProgressCalculatorTests
{
    private static DubbingJob Job() =>
        new(sourceUrl: "https://example.com/v", localFilePath: null, sourceLanguage: null,
            audioLanguage: "vi", subtitleLanguage: "vi", enableDubbing: true);

    private static DubbingJob ThroughPhase1(DubbingJob job)
    {
        job.BeginPhase1Processing();
        foreach (var step in new[]
                 {
                     StepType.Download, StepType.ExtractAudio, StepType.SeparateBgm,
                     StepType.Transcribe, StepType.Translate,
                 })
        {
            var jobStep = job.GetOrCreateStep(step, phase: 1);
            jobStep.Start();
            jobStep.Complete(null);
        }

        job.MarkAwaitingReview();
        return job;
    }

    [Fact]
    public void Given_Completed_Then_Returns100()
    {
        var job = ThroughPhase1(Job());
        job.Confirm();
        job.BeginPhase2Processing();
        job.Complete("s3://out.mp4");

        JobProgressCalculator.Percent(job).Should().Be(100);
    }

    [Fact]
    public void Given_AwaitingReview_Then_ReflectsPhase1Only()
    {
        var job = ThroughPhase1(Job());

        JobProgressCalculator.Percent(job).Should().Be(55);
    }

    [Fact]
    public void Given_ReopenedJob_Then_NotHundred()
    {
        var job = ThroughPhase1(Job());
        job.Confirm();
        job.BeginPhase2Processing();
        foreach (var step in new[] { StepType.Tts, StepType.Mix })
        {
            var jobStep = job.GetOrCreateStep(step, phase: 2);
            jobStep.Start();
            jobStep.Complete(null);
        }

        job.Complete("s3://out.mp4");
        job.ReopenForReview(); // resets Phase-2 steps to Pending

        JobProgressCalculator.Percent(job).Should().BeLessThan(100);
        JobProgressCalculator.Percent(job).Should().Be(55);
    }

    [Fact]
    public void Given_FreshQueuedJob_Then_Zero()
    {
        JobProgressCalculator.Percent(Job()).Should().Be(0);
    }
}
