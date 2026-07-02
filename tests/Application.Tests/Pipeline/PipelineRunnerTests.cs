using Application.Interfaces;
using Application.Pipeline;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Application.Tests.Pipeline;

public class PipelineRunnerTests
{
    private static readonly PipelineRequest Request =
        new(Guid.NewGuid(), "https://youtu.be/x", "vi", "vi");

    private static IWorkspaceManager Workspace()
    {
        var workspace = Substitute.For<IWorkspaceManager>();
        workspace.GetOrCreateWorkspace(Arg.Any<Guid>()).Returns("/work/job");
        return workspace;
    }

    private static IPipelineStep Step(StepType type, StepResult result, List<StepType> log)
    {
        var step = Substitute.For<IPipelineStep>();
        step.StepType.Returns(type);
        step.ExecuteAsync(Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>())
            .Returns(_ => result)
            .AndDoes(_ => log.Add(type));
        return step;
    }

    [Fact]
    public async Task Given_UnorderedSteps_When_RunAsync_Then_RunsInStepTypeOrder()
    {
        // Arrange
        var executed = new List<StepType>();
        var steps = new[]
        {
            Step(StepType.Render, StepResult.Success(), executed),
            Step(StepType.Download, StepResult.Success(), executed),
            Step(StepType.Transcribe, StepResult.Success(), executed),
        };
        var runner = new PipelineRunner(steps, Workspace(), NullLogger<PipelineRunner>.Instance);

        // Act
        await runner.RunAsync(Request, CancellationToken.None);

        // Assert
        executed.Should().Equal(StepType.Download, StepType.Transcribe, StepType.Render);
    }

    [Fact]
    public async Task Given_AFailingStep_When_RunAsync_Then_ThrowsAndStopsAfterIt()
    {
        // Arrange
        var executed = new List<StepType>();
        var steps = new[]
        {
            Step(StepType.Download, StepResult.Success(), executed),
            Step(StepType.ExtractAudio, StepResult.Fail("boom"), executed),
            Step(StepType.Transcribe, StepResult.Success(), executed),
        };
        var runner = new PipelineRunner(steps, Workspace(), NullLogger<PipelineRunner>.Instance);

        // Act
        var act = () => runner.RunAsync(Request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<PipelineExecutionException>();
        ex.Which.Step.Should().Be(StepType.ExtractAudio);
        executed.Should().Equal(StepType.Download, StepType.ExtractAudio); // Transcribe never runs
    }

    [Fact]
    public async Task Given_ASkippedStep_When_RunAsync_Then_ContinuesToNextStep()
    {
        // Arrange
        var executed = new List<StepType>();
        var steps = new[]
        {
            Step(StepType.Tts, StepResult.Skip("dubbing disabled"), executed),
            Step(StepType.Render, StepResult.Success(), executed),
        };
        var runner = new PipelineRunner(steps, Workspace(), NullLogger<PipelineRunner>.Instance);

        // Act
        await runner.RunAsync(Request, CancellationToken.None);

        // Assert
        executed.Should().Equal(StepType.Tts, StepType.Render);
    }
}
