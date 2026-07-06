using Application.Enums;
using Application.Features.Segments.AdjustSegmentTiming;
using Application.Features.Segments.BulkUpdateSegments;
using Application.Features.Segments.GetSegments;
using Application.Features.Segments.UpdateSegment;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/jobs/{jobId:guid}/segments")]
public sealed class SegmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SegmentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get(
        Guid jobId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new GetSegmentsQuery(jobId, page, pageSize), cancellationToken);
        return response.Status == OperationStatus.NotFound ? NotFound() : Ok(response.Segments);
    }

    [HttpPut("{segmentId:guid}")]
    public async Task<IActionResult> Update(
        Guid jobId,
        Guid segmentId,
        [FromBody] UpdateSegmentCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command with { JobId = jobId, SegmentId = segmentId }, cancellationToken);
        return response.Status switch
        {
            OperationStatus.Ok => Ok(response.Segment),
            OperationStatus.NotFound => NotFound(),
            OperationStatus.Conflict => Conflict(response.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [HttpPut("{segmentId:guid}/timing")]
    public async Task<IActionResult> UpdateTiming(
        Guid jobId,
        Guid segmentId,
        [FromBody] AdjustSegmentTimingCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command with { JobId = jobId, SegmentId = segmentId }, cancellationToken);
        return response.Status switch
        {
            OperationStatus.Ok => Ok(response.Segment),
            OperationStatus.NotFound => NotFound(),
            OperationStatus.Conflict => Conflict(response.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [HttpPut]
    public async Task<IActionResult> BulkUpdate(
        Guid jobId,
        [FromBody] BulkUpdateSegmentsCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command with { JobId = jobId }, cancellationToken);
        return response.Status switch
        {
            OperationStatus.Ok => Ok(response.Segments),
            OperationStatus.NotFound => NotFound(response.Error),
            OperationStatus.Conflict => Conflict(response.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
