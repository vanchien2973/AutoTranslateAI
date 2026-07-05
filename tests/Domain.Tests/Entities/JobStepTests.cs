using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Tests.Entities;

public class JobStepTests
{
    private static JobStep NewStep() => new(Guid.NewGuid(), StepType.Tts, phase: 2);

    [Fact]
    public void Given_RunningStep_When_Skip_Then_MovesToSkipped()
    {
        // Arrange
        var step = NewStep();
        step.Start();

        // Act
        step.Skip();

        // Assert
        step.Status.Should().Be(JobStepStatus.Skipped);
    }

    [Fact]
    public void Given_PendingStep_When_Skip_Then_MovesToSkipped()
    {
        // Arrange
        var step = NewStep();

        // Act
        step.Skip();

        // Assert
        step.Status.Should().Be(JobStepStatus.Skipped);
    }

    [Fact]
    public void Given_CompletedStep_When_Skip_Then_ThrowsInvalidStateTransition()
    {
        // Arrange
        var step = NewStep();
        step.Start();
        step.Complete("out.wav");

        // Act
        var act = () => step.Skip();

        // Assert
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Given_FailedStep_When_Start_Then_IncrementsRetryCount()
    {
        // Arrange
        var step = NewStep();
        step.Start();
        step.Fail("boom");

        // Act
        step.Start();

        // Assert
        step.Status.Should().Be(JobStepStatus.Running);
        step.RetryCount.Should().Be(1);
    }

    [Fact]
    public void Given_CompletedStep_When_Reset_Then_MovesToPending()
    {
        // Arrange
        var step = NewStep();
        step.Start();
        step.Complete("out.wav");

        // Act
        step.Reset();

        // Assert
        step.Status.Should().Be(JobStepStatus.Pending);
        step.CompletedAt.Should().BeNull();
    }
}
