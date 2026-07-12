using Application.Features.Publishing.GetPlatformCredentials;
using Application.Features.Publishing.SetPlatformCredential;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/publishing/credentials")]
public sealed class PlatformCredentialsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformCredentialsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetPlatformCredentialsQuery(), cancellationToken);
        return Ok(response.Credentials);
    }

    [HttpPut("{platform}")]
    public async Task<IActionResult> Set(
        PublishPlatform platform,
        [FromBody] SetPlatformCredentialCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command with { Platform = platform }, cancellationToken);
        return Ok(response.Credential);
    }
}
