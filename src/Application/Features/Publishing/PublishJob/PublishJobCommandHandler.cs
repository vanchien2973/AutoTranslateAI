using Application.Interfaces;
using Application.Messaging;
using Domain.Enums;
using MediatR;

namespace Application.Features.Publishing.PublishJob;

public sealed class PublishJobCommandHandler : IRequestHandler<PublishJobCommand, PublishJobResponse>
{
    private readonly IDubbingJobRepository _jobs;
    private readonly IEventPublisher _events;

    public PublishJobCommandHandler(IDubbingJobRepository jobs, IEventPublisher events)
    {
        _jobs = jobs;
        _events = events;
    }

    public async Task<PublishJobResponse> Handle(PublishJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return PublishJobResponse.JobNotFound(request.JobId);
        }

        if (job.Status != JobStatus.Completed || string.IsNullOrEmpty(job.OutputFilePath))
        {
            return PublishJobResponse.NotCompleted();
        }

        if (request.Targets.Count == 0)
        {
            return PublishJobResponse.NoTargets();
        }

        await _events.PublishAsync(new DubbingJobPublishRequested(request.JobId, request.Targets), cancellationToken);
        return PublishJobResponse.Ok();
    }
}
