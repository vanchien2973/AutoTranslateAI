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

    private static IJobStepTracker Tracker(params StepType[] completed)
    {
        var tracker = Substitute.For<IJobStepTracker>();
        tracker.GetCompletedStepsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlySet<StepType>>(completed.ToHashSet()));
        return tracker;
    }

    private static IPipelineStateStore Store(PipelineStateSnapshot? snapshot = null)
    {
        var store = Substitute.For<IPipelineStateStore>();
        store.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(snapshot));
        return store;
    }

    private static PipelineRunner Runner(
        IPipelineStep[] steps,
        IJobStepTracker tracker,
        IPipelineStateStore store,
        IProgressNotifier? notifier = null) =>
        new(steps, Workspace(), tracker, store, notifier ?? Substitute.For<IProgressNotifier>(), NullLogger<PipelineRunner>.Instance);

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
    public async Task Given_UnorderedPhase1Steps_When_RunPhase1_Then_RunsInStepTypeOrder()
    {
        // Arrange
        var executed = new List<StepType>();
        var steps = new[]
        {
            Step(StepType.SeparateBgm, StepResult.Success(), executed),
            Step(StepType.Download, StepResult.Success(), executed),
            Step(StepType.Transcribe, StepResult.Success(), executed),
        };
        var runner = Runner(steps, Tracker(), Store());

        // Act
        await runner.RunAsync(Request, PipelinePhase.Phase1, CancellationToken.None);

        // Assert
        executed.Should().Equal(StepType.Download, StepType.SeparateBgm, StepType.Transcribe);
    }

    [Fact]
    public async Task Given_StepsInBothPhases_When_RunPhase1_Then_RunsOnlyPhase1Steps()
    {
        // Arrange
        var executed = new List<StepType>();
        var steps = new[]
        {
            Step(StepType.Download, StepResult.Success(), executed),   // phase 1
            Step(StepType.Tts, StepResult.Success(), executed),        // phase 2
        };
        var runner = Runner(steps, Tracker(), Store());

        // Act
        await runner.RunAsync(Request, PipelinePhase.Phase1, CancellationToken.None);

        // Assert
        executed.Should().Equal(StepType.Download); // Tts belongs to phase 2, not run here
    }

    [Fact]
    public async Task Given_StepsInBothPhases_When_RunPhase2_Then_RunsOnlyPhase2Steps()
    {
        // Arrange
        var executed = new List<StepType>();
        var steps = new[]
        {
            Step(StepType.Translate, StepResult.Success(), executed),  // phase 1
            Step(StepType.Tts, StepResult.Success(), executed),        // phase 2
            Step(StepType.Render, StepResult.Success(), executed),     // phase 2
        };
        var runner = Runner(steps, Tracker(), Store());

        // Act
        await runner.RunAsync(Request, PipelinePhase.Phase2, CancellationToken.None);

        // Assert
        executed.Should().Equal(StepType.Tts, StepType.Render);
    }

    [Fact]
    public async Task Given_AFailingStep_When_RunAsync_Then_ThrowsAndRecordsFailure()
    {
        // Arrange
        var executed = new List<StepType>();
        var tracker = Tracker();
        var steps = new[]
        {
            Step(StepType.Download, StepResult.Success(), executed),
            Step(StepType.ExtractAudio, StepResult.Fail("boom"), executed),
            Step(StepType.Transcribe, StepResult.Success(), executed),
        };
        var runner = Runner(steps, tracker, Store());

        // Act
        var act = () => runner.RunAsync(Request, PipelinePhase.Phase1, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<PipelineExecutionException>();
        ex.Which.Step.Should().Be(StepType.ExtractAudio);
        executed.Should().Equal(StepType.Download, StepType.ExtractAudio); // Transcribe never runs
        await tracker.Received(1).FailAsync(Request.JobId, StepType.ExtractAudio, "boom", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_ASkippedStep_When_RunAsync_Then_ContinuesAndRecordsSkip()
    {
        // Arrange
        var executed = new List<StepType>();
        var tracker = Tracker();
        var steps = new[]
        {
            Step(StepType.Tts, StepResult.Skip("dubbing disabled"), executed),
            Step(StepType.Mix, StepResult.Success(), executed),
        };
        var runner = Runner(steps, tracker, Store());

        // Act
        await runner.RunAsync(Request, PipelinePhase.Phase2, CancellationToken.None);

        // Assert
        executed.Should().Equal(StepType.Tts, StepType.Mix);
        await tracker.Received(1).SkipAsync(Request.JobId, StepType.Tts, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_StepAlreadyCompleted_When_RunAsync_Then_SkipsItAndResumesRemaining()
    {
        // Arrange
        var executed = new List<StepType>();
        var steps = new[]
        {
            Step(StepType.Download, StepResult.Success(), executed),
            Step(StepType.ExtractAudio, StepResult.Success(), executed),
        };
        var runner = Runner(steps, Tracker(StepType.Download), Store());

        // Act
        await runner.RunAsync(Request, PipelinePhase.Phase1, CancellationToken.None);

        // Assert
        executed.Should().Equal(StepType.ExtractAudio); // Download resumed (skipped), not re-run
    }

    [Fact]
    public async Task Given_PriorSnapshot_When_RunAsync_Then_RestoresContextBeforeSteps()
    {
        // Arrange
        var seen = new List<string?>();
        var snapshot = new PipelineStateSnapshot { AudioPath = "/work/job/audio.wav" };
        var step = Substitute.For<IPipelineStep>();
        step.StepType.Returns(StepType.Transcribe);
        step.ExecuteAsync(Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>())
            .Returns(StepResult.Success())
            .AndDoes(call => seen.Add(call.Arg<PipelineContext>().AudioPath));
        var runner = Runner([step], Tracker(), Store(snapshot));

        // Act
        await runner.RunAsync(Request, PipelinePhase.Phase1, CancellationToken.None);

        // Assert
        seen.Should().ContainSingle().Which.Should().Be("/work/job/audio.wav");
    }

    [Fact]
    public async Task Given_RequestSegments_When_RunAsync_Then_SeedsContextWithThem()
    {
        // Arrange
        var seen = new List<int>();
        var request = Request with
        {
            Segments = [new PipelineSegment { Index = 7, StartTime = 0, EndTime = 1, OriginalText = "hi" }],
        };
        var step = Substitute.For<IPipelineStep>();
        step.StepType.Returns(StepType.Tts);
        step.ExecuteAsync(Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>())
            .Returns(StepResult.Success())
            .AndDoes(call => seen.AddRange(call.Arg<PipelineContext>().Segments.Select(s => s.Index)));
        var runner = Runner([step], Tracker(), Store());

        // Act
        await runner.RunAsync(request, PipelinePhase.Phase2, CancellationToken.None);

        // Assert
        seen.Should().Equal(7);
    }

    [Fact]
    public async Task Given_ASuccessfulStep_When_RunAsync_Then_PersistsSnapshotAfterIt()
    {
        // Arrange
        var executed = new List<StepType>();
        var store = Store();
        var runner = Runner([Step(StepType.Download, StepResult.Success(), executed)], Tracker(), store);

        // Act
        await runner.RunAsync(Request, PipelinePhase.Phase1, CancellationToken.None);

        // Assert
        await store.Received(1).SaveAsync(Request.JobId, Arg.Any<PipelineStateSnapshot>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Given_ASuccessfulStep_When_RunAsync_Then_ReportsProgress()
    {
        // Arrange
        var notifier = Substitute.For<IProgressNotifier>();
        var runner = Runner([Step(StepType.Download, StepResult.Success(), [])], Tracker(), Store(), notifier);

        // Act
        await runner.RunAsync(Request, PipelinePhase.Phase1, CancellationToken.None);

        // Assert
        await notifier.Received().ReportAsync(
            Arg.Is<JobProgress>(p => p.JobId == Request.JobId && p.CurrentStep == nameof(StepType.Download)),
            Arg.Any<CancellationToken>());
    }
}
