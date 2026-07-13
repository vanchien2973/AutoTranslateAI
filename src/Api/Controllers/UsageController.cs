using Application.Features.Usage.GetUsageSummary;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/usage")]
public sealed class UsageController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsageController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Summary([FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new GetUsageSummaryQuery(days), cancellationToken);
        return Ok(response);
    }
}
