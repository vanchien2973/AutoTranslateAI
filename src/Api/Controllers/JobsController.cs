using Application.Messaging;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class JobsController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;

    public JobsController(IPublishEndpoint publishEndpoint) => _publishEndpoint = publishEndpoint;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SourceUrl))
        {
            return BadRequest("SourceUrl is required.");
        }

        var jobId = Guid.NewGuid();
        var audioLanguage = string.IsNullOrWhiteSpace(request.AudioLanguage) ? "vi" : request.AudioLanguage;

        await _publishEndpoint.Publish(
            new DubbingJobRequested(
                jobId,
                request.SourceUrl,
                audioLanguage,
                string.IsNullOrWhiteSpace(request.SubtitleLanguage) ? audioLanguage : request.SubtitleLanguage,
                request.EnableDubbing ?? true),
            cancellationToken);

        return Accepted(new { jobId });
    }
}

public sealed record CreateJobRequest(
    string SourceUrl,
    string? AudioLanguage,
    string? SubtitleLanguage,
    bool? EnableDubbing);
