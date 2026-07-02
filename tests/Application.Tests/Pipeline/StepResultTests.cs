using Application.Pipeline;

namespace Application.Tests.Pipeline;

public class StepResultTests
{
    [Fact]
    public void Given_SuccessFactory_When_Created_Then_IsSuccessOnly()
    {
        // Act
        var result = StepResult.Success();

        // Assert
        result.Outcome.Should().Be(StepOutcome.Success);
        result.IsSuccess.Should().BeTrue();
        result.IsSkipped.Should().BeFalse();
        result.IsFailed.Should().BeFalse();
    }

    [Fact]
    public void Given_SkipFactory_When_Created_Then_IsSkippedAndKeepsReason()
    {
        // Act
        var result = StepResult.Skip("no dubbing requested");

        // Assert
        result.IsSkipped.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("no dubbing requested");
    }

    [Fact]
    public void Given_FailFactory_When_Created_Then_IsFailedAndKeepsError()
    {
        // Act
        var result = StepResult.Fail("ffmpeg exited 1");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Message.Should().Be("ffmpeg exited 1");
    }
}
