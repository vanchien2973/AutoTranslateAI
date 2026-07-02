using Application.Dtos;
using Application.Interfaces;
using Application.Pipeline;
using Domain.Enums;

namespace Workers.Steps;

/// <summary>Step 1: Download the source video to your job workspace.</summary>
public sealed class DownloadStep : IPipelineStep
{
    private readonly IVideoDownloader _downloader;

    public DownloadStep(IVideoDownloader downloader) => _downloader = downloader;

    public StepType StepType => StepType.Download;

    public async Task<StepResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        var result = await _downloader.DownloadAsync(
            new DownloadRequest(context.SourceUrl, context.WorkspacePath), cancellationToken);

        context.SourceVideoPath = result.FilePath;
        return StepResult.Success();
    }
}
