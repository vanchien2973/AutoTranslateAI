using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Jobs.GetJobStatus;

public sealed class GetJobStatusQueryHandler : IRequestHandler<GetJobStatusQuery, GetJobStatusResponse>
{
    private static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromHours(1);

    private readonly IDubbingJobRepository _jobs;
    private readonly IStorageService _storage;

    public GetJobStatusQueryHandler(IDubbingJobRepository jobs, IStorageService storage)
    {
        _jobs = jobs;
        _storage = storage;
    }

    public async Task<GetJobStatusResponse> Handle(GetJobStatusQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return GetJobStatusResponse.NotFound();
        }

        var progress = JobProgressCalculator.Percent(job);

        // Prefer the step actually Running; fall back to the job's last-set step.
        var running = job.Steps.FirstOrDefault(step => step.Status == JobStepStatus.Running);
        var currentStep = running?.StepType.ToString() ?? job.CurrentStep?.ToString();

        // Embed a ready-to-use presigned download link once the output exists.
        var downloadUrl = IsOutputReady(job)
            ? await _storage.GetPresignedUrlAsync(OutputStorageKey.For(job.Id), DownloadUrlLifetime, cancellationToken)
            : null;

        var steps = job.Steps
            .OrderBy(step => step.StepType)
            .Select(step => new JobStepDto(
                step.StepType.ToString(),
                step.Status.ToString(),
                step.Phase,
                step.DurationMs,
                step.RetryCount,
                step.ErrorMessage))
            .ToList();

        var dto = new JobStatusDto(
            job.Id,
            job.Status.ToString(),
            job.AudioLanguage,
            job.SubtitleLanguage,
            job.EnableDubbing,
            currentStep,
            progress,
            job.ErrorMessage,
            job.OutputFilePath,
            downloadUrl,
            job.Segments.Count,
            job.Segments.Count(segment => segment.IsEdited),
            job.CreatedAt,
            job.StartedAt,
            job.ReviewReadyAt,
            job.ConfirmedAt,
            job.CompletedAt,
            steps,
            job.SubtitleMode,
            job.SubtitleFontFamily,
            job.SubtitleFontSize,
            job.SubtitlePosition,
            job.SubtitleBold,
            job.SubtitleItalic);

        return GetJobStatusResponse.Ok(dto);
    }

    private static bool IsOutputReady(DubbingJob job) =>
        job.Status == JobStatus.Completed && !string.IsNullOrEmpty(job.OutputFilePath);
}
