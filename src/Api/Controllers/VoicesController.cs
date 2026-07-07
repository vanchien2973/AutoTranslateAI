using Application.Features.Voices.GetSupportedLanguages;
using Application.Features.Voices.GetVoices;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/voices")]
public sealed class VoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public VoicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string language, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetVoicesQuery(language), cancellationToken);
        return Ok(response.Voices);
    }

    [HttpGet("languages")]
    public async Task<IActionResult> Languages(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetSupportedLanguagesQuery(), cancellationToken);
        return Ok(response.AudioLanguages);
    }
}
