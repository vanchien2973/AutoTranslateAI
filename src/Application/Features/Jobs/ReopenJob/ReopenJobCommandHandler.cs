using Application.Interfaces;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Jobs.ReopenJob;

public sealed class ReopenJobCommandHandler : IRequestHandler<ReopenJobCommand, ReopenJobResponse>
{
    private readonly IDubbingJobRepository _jobs;

    public ReopenJobCommandHandler(IDubbingJobRepository jobs) => _jobs = jobs;

    public async Task<ReopenJobResponse> Handle(ReopenJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return ReopenJobResponse.NotFound();
        }

        try
        {
            job.ReopenForReview();
            await _jobs.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidStateTransitionException exception)
        {
            return ReopenJobResponse.Conflict(exception.Message);
        }

        return ReopenJobResponse.Ok(job.Id, job.Status.ToString());
    }
}
