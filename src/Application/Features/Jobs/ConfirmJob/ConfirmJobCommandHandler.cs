using Application.Interfaces;
using Application.Messaging;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.Jobs.ConfirmJob;

public sealed class ConfirmJobCommandHandler : IRequestHandler<ConfirmJobCommand, ConfirmJobResponse>
{
    private readonly IDubbingJobRepository _jobs;
    private readonly IEventPublisher _events;

    public ConfirmJobCommandHandler(IDubbingJobRepository jobs, IEventPublisher events)
    {
        _jobs = jobs;
        _events = events;
    }

    public async Task<ConfirmJobResponse> Handle(ConfirmJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return ConfirmJobResponse.NotFound();
        }

        try
        {
            job.Confirm();
            await _jobs.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidStateTransitionException exception)
        {
            return ConfirmJobResponse.Conflict(exception.Message);
        }

        // Transcript approved: kick off Phase 2 (TTS → Mix → Render → Upload).
        await _events.PublishAsync(new DubbingJobConfirmed(job.Id), cancellationToken);

        return ConfirmJobResponse.Ok(job.Id, job.Status.ToString());
    }
}
