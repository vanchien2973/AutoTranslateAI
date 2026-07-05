using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/jobs/{jobId:guid}/segments")]
public sealed class SegmentsController : ControllerBase
{
    private const string NotAwaitingReview = "Segments can only be edited while the job is awaiting review.";

    private readonly IDubbingJobRepository _jobs;

    public SegmentsController(IDubbingJobRepository jobs) => _jobs = jobs;

    [HttpGet]
    public async Task<IActionResult> Get(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        var segments = job.Segments.OrderBy(segment => segment.SegmentIndex).Select(ToDto);
        return Ok(segments);
    }

    [HttpPut("{segmentId:guid}")]
    public async Task<IActionResult> Update(
        Guid jobId,
        Guid segmentId,
        [FromBody] UpdateSegmentRequest request,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        if (job.Status != JobStatus.AwaitingReview)
        {
            return Conflict(NotAwaitingReview);
        }

        var segment = job.Segments.FirstOrDefault(s => s.Id == segmentId);
        if (segment is null)
        {
            return NotFound();
        }

        ApplyEdit(segment, request.AudioTextEdited, request.SubtitleTextEdited, request.AssignedVoice);
        await _jobs.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(segment));
    }

    [HttpPut]
    public async Task<IActionResult> BulkUpdate(
        Guid jobId,
        [FromBody] BulkUpdateSegmentsRequest request,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        if (job.Status != JobStatus.AwaitingReview)
        {
            return Conflict(NotAwaitingReview);
        }

        var byId = job.Segments.ToDictionary(segment => segment.Id);
        foreach (var edit in request.Segments)
        {
            if (!byId.TryGetValue(edit.SegmentId, out var segment))
            {
                return NotFound($"Segment {edit.SegmentId} not found in job {jobId}.");
            }

            ApplyEdit(segment, edit.AudioTextEdited, edit.SubtitleTextEdited, edit.AssignedVoice);
        }

        await _jobs.SaveChangesAsync(cancellationToken);
        return Ok(job.Segments.OrderBy(segment => segment.SegmentIndex).Select(ToDto));
    }

    // Only touch a field when the caller supplied it (null = leave unchanged).
    private static void ApplyEdit(Segment segment, string? audioText, string? subtitleText, string? assignedVoice)
    {
        if (audioText is not null)
        {
            segment.EditAudioText(audioText);
        }

        if (subtitleText is not null)
        {
            segment.EditSubtitleText(subtitleText);
        }

        if (assignedVoice is not null)
        {
            segment.AssignVoice(segment.SpeakerLabel, assignedVoice);
        }
    }

    private static SegmentDto ToDto(Segment s) => new(
        s.Id,
        s.SegmentIndex,
        s.StartTime,
        s.EndTime,
        s.OriginalText,
        s.AudioTextAi,
        s.AudioTextEdited,
        s.SubtitleTextAi,
        s.SubtitleTextEdited,
        s.TtsText,
        s.SubtitleText,
        s.AssignedVoice,
        s.IsEdited,
        s.NeedsTtsRegenerate);
}

