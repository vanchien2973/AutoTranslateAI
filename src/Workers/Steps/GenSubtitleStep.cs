using Application.Helpers;
using Application.Interfaces;
using Application.Pipeline;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Workers.Steps;

/// <summary>
/// Step 6: Generate the .srt file from the segment when <c>SubtitleMode != None</c>. The Render step will mux (softsub) or burn (hardsub) this file into the video. Skip this step if 
/// subtitles are not enabled or there is no subtitle content.
/// </summary>
public sealed class GenSubtitleStep : IPipelineStep
{
    private readonly IWorkspaceManager _workspace;
    private readonly ILogger<GenSubtitleStep> _logger;

    public GenSubtitleStep(IWorkspaceManager workspace, ILogger<GenSubtitleStep> logger)
    {
        _workspace = workspace;
        _logger = logger;
    }

    public StepType StepType => StepType.GenSubtitle;

    public async Task<StepResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        if (context.SubtitleMode == SubtitleMode.None)
        {
            return StepResult.Skip("Subtitles disabled (SubtitleMode = None).");
        }

        if (context.Segments.Count == 0)
        {
            return StepResult.Skip("No segments to render as subtitles.");
        }

        var srt = SrtBuilder.Build(context.Segments);
        if (string.IsNullOrWhiteSpace(srt))
        {
            return StepResult.Skip("No subtitle text to render.");
        }

        var subtitlePath = _workspace.GetArtifactPath(context.JobId, "subtitle.srt");
        await File.WriteAllTextAsync(subtitlePath, srt, cancellationToken);
        context.SubtitlePath = subtitlePath;

        _logger.LogInformation(
            "Job {JobId}: generated subtitle ({Mode}) at {Path}", context.JobId, context.SubtitleMode, subtitlePath);
        return StepResult.Success();
    }
}
