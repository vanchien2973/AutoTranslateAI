using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Pipeline;

public sealed class PipelineRunner
{
    private readonly IReadOnlyList<IPipelineStep> _steps;
    private readonly IWorkspaceManager _workspace;
    private readonly IJobStepTracker _stepTracker;
    private readonly IPipelineStateStore _stateStore;
    private readonly ILogger<PipelineRunner> _logger;

    public PipelineRunner(
        IEnumerable<IPipelineStep> steps,
        IWorkspaceManager workspace,
        IJobStepTracker stepTracker,
        IPipelineStateStore stateStore,
        ILogger<PipelineRunner> logger)
    {
        _steps = steps.OrderBy(step => (int)step.StepType).ToList();
        _workspace = workspace;
        _stepTracker = stepTracker;
        _stateStore = stateStore;
        _logger = logger;
    }

    public async Task<PipelineContext> RunAsync(PipelineRequest request, CancellationToken cancellationToken)
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
        var snapshot = await _stateStore.LoadAsync(request.JobId, cancellationToken);
        snapshot?.ApplyTo(context);
        var completed = await _stepTracker.GetCompletedStepsAsync(request.JobId, cancellationToken);

        _logger.LogInformation(
            "Job {JobId}: running {Count} steps ({Done} already completed)",
            request.JobId, _steps.Count, completed.Count);

        foreach (var step in _steps)
        {
            if (completed.Contains(step.StepType))
            {
                _logger.LogInformation("Job {JobId}: step {Step} already done, skipping (resume)", request.JobId, step.StepType);
                continue;
            }

            await _stepTracker.StartAsync(request.JobId, step.StepType, cancellationToken);

            StepResult result;
            try
            {
                result = await step.ExecuteAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                // Record the failure point, then let the exception bubble so MassTransit retries the message.
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

            // Persist the snapshot after every step so a later retry resumes with all artifacts intact.
            await _stateStore.SaveAsync(request.JobId, PipelineStateSnapshot.Capture(context), cancellationToken);
        }

        _logger.LogInformation("Job {JobId}: pipeline complete, output at {Url}", request.JobId, context.OutputUrl);
        return context;
    }
}
