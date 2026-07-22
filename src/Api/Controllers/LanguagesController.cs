using Application.Features.Languages.GetTranslationLanguages;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/languages")]
public sealed class LanguagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public LanguagesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("translation")]
    public async Task<IActionResult> Translation(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetTranslationLanguagesQuery(), cancellationToken);
        return Ok(response.SubtitleLanguages);
    }
}
