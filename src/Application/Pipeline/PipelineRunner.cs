using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Pipeline;

public sealed class PipelineRunner
{
    private readonly IReadOnlyList<IPipelineStep> _steps;
    private readonly IWorkspaceManager _workspace;
    private readonly IJobStepTracker _stepTracker;
    private readonly IPipelineStateStore _stateStore;
    private readonly IProgressNotifier _progress;
    private readonly ILogger<PipelineRunner> _logger;

    public PipelineRunner(
        IEnumerable<IPipelineStep> steps,
        IWorkspaceManager workspace,
        IJobStepTracker stepTracker,
        IPipelineStateStore stateStore,
        IProgressNotifier progress,
        ILogger<PipelineRunner> logger)
    {
        _steps = steps.OrderBy(step => (int)step.StepType).ToList();
        _workspace = workspace;
        _stepTracker = stepTracker;
        _stateStore = stateStore;
        _progress = progress;
        _logger = logger;
    }

    public async Task<PipelineContext> RunAsync(PipelineRequest request, PipelinePhase phase, CancellationToken cancellationToken)
    {
        var context = new PipelineContext
        {
            JobId = request.JobId,
            WorkspacePath = _workspace.GetOrCreateWorkspace(request.JobId),
            SourceUrl = request.SourceUrl,
            AudioLanguage = request.AudioLanguage,
            SubtitleLanguage = request.SubtitleLanguage,
            EnableDubbing = request.EnableDubbing,
            DefaultVoiceGender = request.DefaultVoiceGender,
        };

        // Resume: rehydrate artifacts/segments from the last snapshot and learn which steps already finished.
        // Phase 2 relies on this to pick up Phase 1's segments + audio artifacts.
        var snapshot = await _stateStore.LoadAsync(request.JobId, cancellationToken);
        snapshot?.ApplyTo(context);

        // The user-reviewed segments (from the DB) win over the snapshot copy so edits reach TTS/subtitles.
        if (request.Segments is { Count: > 0 })
        {
            context.Segments.Clear();
            context.Segments.AddRange(request.Segments);
        }

        var completed = await _stepTracker.GetCompletedStepsAsync(request.JobId, cancellationToken);

        // Only run steps belonging to this phase (Phase 1: Download..Translate, Phase 2: Tts..Publish).
        var stepsToRun = _steps.Where(step => StepPhases.IsIn(step.StepType, phase)).ToList();
        var status = phase == PipelinePhase.Phase1 ? "ProcessingPhase1" : "ProcessingPhase2";

        // Progress achieved so far = highest percent among already-finished steps.
        var percentSoFar = completed.Count == 0
            ? 0
            : stepsToRun.Where(step => completed.Contains(step.StepType))
                .Select(step => PipelineProgress.PercentAfter(step.StepType))
                .DefaultIfEmpty(0)
                .Max();

        _logger.LogInformation(
            "Job {JobId}: running {Phase} ({Count} steps, {Done} already completed)",
            request.JobId, phase, stepsToRun.Count, completed.Count);

        foreach (var step in stepsToRun)
        {
            if (completed.Contains(step.StepType))
            {
                _logger.LogInformation("Job {JobId}: step {Step} already done, skipping (resume)", request.JobId, step.StepType);
                continue;
            }

            await _stepTracker.StartAsync(request.JobId, step.StepType, cancellationToken);
            await _progress.ReportAsync(new JobProgress(request.JobId, status, step.StepType.ToString(), percentSoFar), cancellationToken);

            StepResult result;
            try
            {
                result = await step.ExecuteAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                await _stepTracker.FailAsync(request.JobId, step.StepType, ex.Message, cancellationToken);
                _logger.LogError(ex, "Job {JobId}: step {Step} threw", request.JobId, step.StepType);
                throw;
            }

            if (result.IsFailed)
            {
                await _stepTracker.FailAsync(request.JobId, step.StepType, result.Message ?? "failed", cancellationToken);
                _logger.LogError("Job {JobId}: step {Step} failed: {Message}", request.JobId, step.StepType, result.Message);
                throw new PipelineExecutionException(step.StepType, result.Message);
            }

            if (result.IsSkipped)
            {
                await _stepTracker.SkipAsync(request.JobId, step.StepType, cancellationToken);
                _logger.LogInformation("Job {JobId}: step {Step} skipped ({Message})", request.JobId, step.StepType, result.Message);
            }
            else
            {
                await _stepTracker.CompleteAsync(request.JobId, step.StepType, null, cancellationToken);
                _logger.LogInformation("Job {JobId}: step {Step} done", request.JobId, step.StepType);
            }

            percentSoFar = PipelineProgress.PercentAfter(step.StepType);
            await _progress.ReportAsync(new JobProgress(request.JobId, status, step.StepType.ToString(), percentSoFar), cancellationToken);
            await _stateStore.SaveAsync(request.JobId, PipelineStateSnapshot.Capture(context), cancellationToken);
        }

        _logger.LogInformation("Job {JobId}: {Phase} steps complete", request.JobId, phase);
        return context;
    }
}
