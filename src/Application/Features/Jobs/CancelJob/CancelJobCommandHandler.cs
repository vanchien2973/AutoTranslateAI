using Application.Interfaces;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Jobs.CancelJob;

public sealed class CancelJobCommandHandler : IRequestHandler<CancelJobCommand, CancelJobResponse>
{
    private readonly IDubbingJobRepository _jobs;

    public CancelJobCommandHandler(IDubbingJobRepository jobs) => _jobs = jobs;

    public async Task<CancelJobResponse> Handle(CancelJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return CancelJobResponse.NotFound();
        }

        try
        {
            job.Cancel();
            await _jobs.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidStateTransitionException exception)
        {
            return CancelJobResponse.Conflict(exception.Message);
        }

        return CancelJobResponse.Ok(job.Id, job.Status.ToString());
    }
}
