using Application.Enums;
using Application.Features.Review.ApplyProposal;
using Application.Features.Review.GetReviewHistory;
using Application.Features.Review.ReviewChat;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/jobs/{jobId:guid}/review")]
public sealed class ReviewController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewController(IMediator mediator) => _mediator = mediator;

    [HttpPost("chat")]
    public async Task<IActionResult> Chat(Guid jobId, [FromBody] ReviewChatCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command with { JobId = jobId }, cancellationToken);
        return response.Status switch
        {
            ReviewChatStatus.Ok => Ok(response),
            ReviewChatStatus.JobNotFound => NotFound(response.Error),
            ReviewChatStatus.NotAwaitingReview => Conflict(response.Error),
            ReviewChatStatus.InvalidResponse => StatusCode(StatusCodes.Status502BadGateway, response.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [HttpGet("chat")]
    public async Task<IActionResult> History(
        Guid jobId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new GetReviewHistoryQuery(jobId, page, pageSize), cancellationToken));

    [HttpPost("chat/{proposalId:guid}/apply")]
    public async Task<IActionResult> Apply(Guid jobId, Guid proposalId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ApplyProposalCommand(jobId, proposalId), cancellationToken);
        return response.Status switch
        {
            ApplyProposalStatus.Ok => Ok(response.Segment),
            ApplyProposalStatus.JobNotFound => NotFound(response.Error),
            ApplyProposalStatus.SegmentNotFound => NotFound(response.Error),
            ApplyProposalStatus.NotAwaitingReview => Conflict(response.Error),
            ApplyProposalStatus.ProposalNotFound => NotFound(response.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
