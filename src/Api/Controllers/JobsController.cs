using Application.Interfaces;
using Application.Messaging;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class JobsController : ControllerBase
{
    private const int TotalPipelineSteps = 9;
    private static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromHours(1);

    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDubbingJobRepository _jobs;
    private readonly IStorageService _storage;

    public JobsController(IPublishEndpoint publishEndpoint, IDubbingJobRepository jobs, IStorageService storage)
    {
        _publishEndpoint = publishEndpoint;
        _jobs = jobs;
        _storage = storage;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        var finished = job.Steps.Count(step => step.Status is JobStepStatus.Completed or JobStepStatus.Skipped);
        var progress = job.Status == JobStatus.Completed
            ? 100
            : Math.Min(99, finished * 100 / TotalPipelineSteps);

        // Prefer the step actually Running; fall back to the job's last-set step.
        var running = job.Steps.FirstOrDefault(step => step.Status == JobStepStatus.Running);
        var currentStep = running?.StepType.ToString() ?? job.CurrentStep?.ToString();

        // Embed a ready-to-use presigned download link once the output exists.
        var downloadUrl = IsOutputReady(job)
            ? await PresignedOutputUrlAsync(job.Id, cancellationToken)
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
            steps);

        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SourceUrl))
        {
            return BadRequest("SourceUrl is required.");
        }

        var audioLanguage = string.IsNullOrWhiteSpace(request.AudioLanguage) ? "vi" : request.AudioLanguage;
        var subtitleLanguage = string.IsNullOrWhiteSpace(request.SubtitleLanguage) ? audioLanguage : request.SubtitleLanguage;
        var enableDubbing = request.EnableDubbing ?? true;

        // Persist the job first so JobSteps have a parent row and resume/tracking works from the first run.
        var job = new DubbingJob(
            sourceUrl: request.SourceUrl,
            localFilePath: null,
            sourceLanguage: null,
            audioLanguage: audioLanguage,
            subtitleLanguage: subtitleLanguage,
            enableDubbing: enableDubbing);
        await _jobs.AddAsync(job, cancellationToken);

        await _publishEndpoint.Publish(
            new DubbingJobRequested(job.Id, request.SourceUrl, audioLanguage, subtitleLanguage, enableDubbing),
            cancellationToken);

        return Accepted(new { jobId = job.Id });
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        if (!IsOutputReady(job))
        {
            return Conflict("Output is not ready yet.");
        }

        var url = await PresignedOutputUrlAsync(id, cancellationToken);
        return Ok(new { url, expiresInSeconds = (int)DownloadUrlLifetime.TotalSeconds });
    }

    private static bool IsOutputReady(DubbingJob job) =>
        job.Status == JobStatus.Completed && !string.IsNullOrEmpty(job.OutputFilePath);

    private Task<string> PresignedOutputUrlAsync(Guid jobId, CancellationToken cancellationToken) =>
        _storage.GetPresignedUrlAsync(OutputStorageKey.For(jobId), DownloadUrlLifetime, cancellationToken);

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        try
        {
            job.Confirm();
            await _jobs.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidStateTransitionException ex)
        {
            return Conflict(ex.Message);
        }

        // Now that the transcript is approved, kick off Phase 2 (TTS → Mix → Render → Upload).
        await _publishEndpoint.Publish(new DubbingJobConfirmed(job.Id), cancellationToken);

        return Ok(new { jobId = job.Id, status = job.Status.ToString() });
    }

    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        TransitionAsync(id, job => job.Cancel(), cancellationToken);

    [HttpPost("{id:guid}/reopen")]
    public Task<IActionResult> Reopen(Guid id, CancellationToken cancellationToken) =>
        TransitionAsync(id, job => job.ReopenForReview(), cancellationToken);

    // Loads the job, applies a state-machine transition, and saves with RowVersion optimistic concurrency.
    private async Task<IActionResult> TransitionAsync(Guid id, Action<DubbingJob> transition, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        try
        {
            transition(job);
            await _jobs.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidStateTransitionException ex)
        {
            return Conflict(ex.Message);
        }

        return Ok(new { jobId = job.Id, status = job.Status.ToString() });
    }
}

