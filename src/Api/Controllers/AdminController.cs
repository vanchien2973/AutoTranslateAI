using Application.Features.Admin.CleanupStorage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpPost("storage/cleanup")]
    public async Task<IActionResult> CleanupStorage(
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new CleanupStorageCommand(dryRun), cancellationToken);
        return Ok(response);
    }
}
