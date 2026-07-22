using Application.Features.Media.UploadLogo;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/media")]
public sealed class MediaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MediaController(IMediator mediator) => _mediator = mediator;

    [HttpPost("logo")]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Choose an image file to upload.");
        }

        await using var content = file.OpenReadStream();
        var response = await _mediator.Send(
            new UploadLogoCommand(content, file.FileName, file.ContentType ?? string.Empty, file.Length),
            cancellationToken);

        return Ok(response);
    }
}
