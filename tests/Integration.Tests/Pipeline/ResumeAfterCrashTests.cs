using Application.Interfaces;
using Application.Pipeline;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace Integration.Tests.Pipeline;

[Trait("Category", "Integration")]
public sealed class ResumeAfterCrashTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private string _workspaceRoot = string.Empty;
    private IDbContextFactory<AppDbContext> _dbFactory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        _workspaceRoot = Path.Combine(Path.GetTempPath(), "ata-resume-" + Guid.NewGuid());
        _dbFactory = new TestDbContextFactory(_postgres.GetConnectionString());
    }

    private sealed class TestDbContextFactory(string connectionString) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        if (Directory.Exists(_workspaceRoot))
        {
            Directory.Delete(_workspaceRoot, recursive: true);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Given_WorkerKilledMidPipeline_When_MessageRedelivered_Then_ResumesFromUnfinishedStepOnly()
    {
        // Arrange
        var jobId = await SeedJobAsync();
        var workspace = new TempWorkspace(_workspaceRoot);
        var seenAudioPath = new List<string?>();

        var download = new RecordingStep(StepType.Download, ctx => ctx.SourceVideoPath = "video.mp4");
        var extract = new RecordingStep(StepType.ExtractAudio, ctx => ctx.AudioPath = "audio.wav");
        // Transcribe "crashes" on its first invocation, then succeeds on the retry (worker restarted).
        var transcribe = new RecordingStep(
            StepType.Transcribe,
            ctx => seenAudioPath.Add(ctx.AudioPath),
            throwOnCall: 1);
        var steps = new IPipelineStep[] { download, extract, transcribe };

        var request = new PipelineRequest(jobId, "https://youtu.be/x", "vi", "vi");

        // Act 1: first delivery runs Download + ExtractAudio, then Transcribe throws (the "kill").
        var runner1 = MakeRunner(steps, workspace);
        var crash = () => runner1.RunAsync(request, PipelinePhase.Phase1, CancellationToken.None);
        await crash.Should().ThrowAsync<InvalidOperationException>();

        var afterCrash = await StepStatusesAsync(jobId);

        // Act 2: message redelivered to a "restarted" worker, same workspace/DB state.
        var runner2 = MakeRunner(steps, workspace);
        await runner2.RunAsync(request, PipelinePhase.Phase1, CancellationToken.None);

        var afterResume = await StepStatusesAsync(jobId);

        // Assert
        // First run persisted the two finished steps and marked the crashed one Failed.
        afterCrash[StepType.Download].Should().Be(JobStepStatus.Completed);
        afterCrash[StepType.ExtractAudio].Should().Be(JobStepStatus.Completed);
        afterCrash[StepType.Transcribe].Should().Be(JobStepStatus.Failed);

        // The proof: finished steps were NOT re-executed on resume; only Transcribe ran again.
        download.Calls.Should().Be(1);
        extract.Calls.Should().Be(1);
        transcribe.Calls.Should().Be(2);

        // Resume rehydrated artifacts from the snapshot, so Transcribe saw ExtractAudio's output.
        seenAudioPath.Should().ContainSingle().Which.Should().Be("audio.wav");

        // Everything ends Completed, and the resumed step recorded a retry.
        afterResume.Values.Should().AllBeEquivalentTo(JobStepStatus.Completed);
        var transcribeStep = await LoadStepAsync(jobId, StepType.Transcribe);
        transcribeStep.RetryCount.Should().Be(1);
    }

    private PipelineRunner MakeRunner(IPipelineStep[] steps, IWorkspaceManager workspace) =>
        new(steps, workspace, new JobStepTracker(_dbFactory), new FilePipelineStateStore(workspace),
            Substitute.For<IProgressNotifier>(), NullLogger<PipelineRunner>.Instance);

    private async Task<Guid> SeedJobAsync()
    {
        await using var db = CreateDbContext();
        var job = new DubbingJob("https://youtu.be/x", null, "en", "vi", "vi", true);
        db.DubbingJobs.Add(job);
        await db.SaveChangesAsync();
        return job.Id;
    }

    private async Task<Dictionary<StepType, JobStepStatus>> StepStatusesAsync(Guid jobId)
    {
        await using var db = CreateDbContext();
        return await db.JobSteps
            .Where(s => s.JobId == jobId)
            .ToDictionaryAsync(s => s.StepType, s => s.Status);
    }

    private async Task<JobStep> LoadStepAsync(Guid jobId, StepType stepType)
    {
        await using var db = CreateDbContext();
        return await db.JobSteps.FirstAsync(s => s.JobId == jobId && s.StepType == stepType);
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }

    // A fake pipeline step that counts executions, mutates the context, and can throw on a chosen attempt.
    private sealed class RecordingStep : IPipelineStep
    {
        private readonly Action<PipelineContext> _onExecute;
        private readonly int? _throwOnCall;

        public RecordingStep(StepType stepType, Action<PipelineContext> onExecute, int? throwOnCall = null)
        {
            StepType = stepType;
            _onExecute = onExecute;
            _throwOnCall = throwOnCall;
        }

        public StepType StepType { get; }
        public int Calls { get; private set; }

        public Task<StepResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken)
        {
            Calls++;
            if (_throwOnCall == Calls)
            {
                throw new InvalidOperationException($"simulated crash in {StepType} on call {Calls}");
            }

            _onExecute(context);
            return Task.FromResult(StepResult.Success());
        }
    }

    // Minimal workspace over a temp directory (stands in for the local volume that survives a restart).
    private sealed class TempWorkspace : IWorkspaceManager
    {
        private readonly string _root;

        public TempWorkspace(string root) => _root = root;

        public string GetOrCreateWorkspace(Guid jobId)
        {
            var path = Path.Combine(_root, jobId.ToString());
            Directory.CreateDirectory(path);
            return path;
        }

        public string GetArtifactPath(Guid jobId, string relativePath)
        {
            var dir = Path.Combine(_root, jobId.ToString());
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, relativePath);
        }

        public void Cleanup(Guid jobId)
        {
        }
    }
}
