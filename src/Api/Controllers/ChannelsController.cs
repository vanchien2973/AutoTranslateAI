using Application.Features.Publishing.ConnectChannel;
using Application.Features.Publishing.GetChannelAuthUrl;
using Application.Features.Publishing.GetChannels;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/publishing/channels")]
public sealed class ChannelsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChannelsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetChannelsQuery(), cancellationToken);
        return Ok(response.Channels);
    }

    [HttpGet("auth-url")]
    public async Task<IActionResult> AuthUrl(
        [FromQuery] PublishPlatform platform,
        [FromQuery] string redirectUri,
        [FromQuery] string? state,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetChannelAuthUrlQuery(platform, redirectUri, state), cancellationToken);
        return response.Status switch
        {
            AuthUrlStatus.Ok => Ok(new { url = response.Url, state = response.State }),
            AuthUrlStatus.CredentialsMissing => Conflict(response.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectChannelCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);
        return response.Status switch
        {
            ConnectChannelStatus.Ok => Ok(response.Connection),
            ConnectChannelStatus.CredentialsMissing => Conflict(response.Error),
            ConnectChannelStatus.ExchangeFailed => StatusCode(StatusCodes.Status502BadGateway, response.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
