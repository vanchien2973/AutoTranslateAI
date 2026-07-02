using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Pipeline;

public sealed class PipelineRunner
{
    private readonly IReadOnlyList<IPipelineStep> _steps;
    private readonly IWorkspaceManager _workspace;
    private readonly ILogger<PipelineRunner> _logger;

    public PipelineRunner(
        IEnumerable<IPipelineStep> steps,
        IWorkspaceManager workspace,
        ILogger<PipelineRunner> logger)
    {
        _steps = steps.OrderBy(step => (int)step.StepType).ToList();
        _workspace = workspace;
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

        _logger.LogInformation("Job {JobId}: running {Count} pipeline steps", request.JobId, _steps.Count);

        foreach (var step in _steps)
        {
            var result = await step.ExecuteAsync(context, cancellationToken);

            if (result.IsFailed)
            {
                _logger.LogError("Job {JobId}: step {Step} failed: {Message}", request.JobId, step.StepType, result.Message);
                throw new PipelineExecutionException(step.StepType, result.Message);
            }

            if (result.IsSkipped)
            {
                _logger.LogInformation("Job {JobId}: step {Step} skipped ({Message})", request.JobId, step.StepType, result.Message);
            }
            else
            {
                _logger.LogInformation("Job {JobId}: step {Step} done", request.JobId, step.StepType);
            }
        }

        _logger.LogInformation("Job {JobId}: pipeline complete, output at {Url}", request.JobId, context.OutputUrl);
        return context;
    }
}
