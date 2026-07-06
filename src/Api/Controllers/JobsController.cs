using Application.Enums;
using Application.Features.Jobs.CancelJob;
using Application.Features.Jobs.ConfirmJob;
using Application.Features.Jobs.CreateJob;
using Application.Features.Jobs.GetJobDownload;
using Application.Features.Jobs.GetJobs;
using Application.Features.Jobs.GetJobStatus;
using Application.Features.Jobs.ReopenJob;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class JobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new GetJobsQuery(page, pageSize), cancellationToken);
        return Ok(response.Jobs);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetJobStatusQuery(id), cancellationToken);
        return response.Status == OperationStatus.NotFound ? NotFound() : Ok(response.Job);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);
        return Accepted(new { jobId = response.JobId });
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetJobDownloadQuery(id), cancellationToken);
        return response.Status switch
        {
            OperationStatus.Ok => Ok(new { url = response.Url, expiresInSeconds = response.ExpiresInSeconds }),
            OperationStatus.NotFound => NotFound(),
            OperationStatus.Conflict => Conflict(response.Error),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [HttpPost("{id:guid}/confirm")]
    public Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken) =>
        TransitionAsync(new ConfirmJobCommand(id), response => (response.Status, response.JobId, response.JobStatus, response.Error), cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        TransitionAsync(new CancelJobCommand(id), response => (response.Status, response.JobId, response.JobStatus, response.Error), cancellationToken);

    [HttpPost("{id:guid}/reopen")]
    public Task<IActionResult> Reopen(Guid id, CancellationToken cancellationToken) =>
        TransitionAsync(new ReopenJobCommand(id), response => (response.Status, response.JobId, response.JobStatus, response.Error), cancellationToken);

    // Confirm/Cancel/Reopen share the same HTTP mapping over their (Status, JobId, JobStatus, Error) shape.
    private async Task<IActionResult> TransitionAsync<TResponse>(
        IRequest<TResponse> command,
        Func<TResponse, (OperationStatus Status, Guid JobId, string? JobStatus, string? Error)> project,
        CancellationToken cancellationToken)
    {
        var (status, jobId, jobStatus, error) = project(await _mediator.Send(command, cancellationToken));
        return status switch
        {
            OperationStatus.Ok => Ok(new { jobId, status = jobStatus }),
            OperationStatus.NotFound => NotFound(),
            OperationStatus.Conflict => Conflict(error),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
