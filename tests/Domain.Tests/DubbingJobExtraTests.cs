using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Domain.Tests;

public class DubbingJobExtraTests
{
    [Fact]
    public void Constructor_WithoutSourceOrFile_Throws()
    {
        var act = () => new DubbingJob(null, null, "en", "vi", "vi", true);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Constructor_WithoutAudioLanguage_Throws()
    {
        var act = () => new DubbingJob("https://x", null, "en", "", "vi", true);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void UpdateProgress_SetsStepAndClampsPercent()
    {
        var job = new DubbingJob("https://x", null, "en", "vi", "vi", true);

        job.UpdateProgress(StepType.Tts, 150);

        job.CurrentStep.Should().Be(StepType.Tts);
        job.ProgressPercent.Should().Be(100); // clamped
        job.RowVersion.Should().Be(0);
    }
}
