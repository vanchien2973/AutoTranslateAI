using Application.Interfaces;
using Application.Messaging;
using Domain.Entities;
using Domain.Exceptions;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class JobsController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDubbingJobRepository _jobs;

    public JobsController(IPublishEndpoint publishEndpoint, IDubbingJobRepository jobs)
    {
        _publishEndpoint = publishEndpoint;
        _jobs = jobs;
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

    [HttpPost("{id:guid}/confirm")]
    public Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken) =>
        TransitionAsync(id, job => job.Confirm(), cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        TransitionAsync(id, job => job.Cancel(), cancellationToken);

    // Loads the job, applies a state-machine transition, and saves with xmin optimistic concurrency.
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

public sealed record CreateJobRequest(
    string SourceUrl,
    string? AudioLanguage,
    string? SubtitleLanguage,
    bool? EnableDubbing);
