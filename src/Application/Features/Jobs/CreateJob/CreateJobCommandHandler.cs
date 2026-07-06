using Application.Interfaces;
using Application.Messaging;
using Domain.Entities;
using MediatR;

namespace Application.Features.Jobs.CreateJob;

public sealed class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, CreateJobResponse>
{
    private readonly IDubbingJobRepository _jobs;
    private readonly IEventPublisher _events;

    public CreateJobCommandHandler(IDubbingJobRepository jobs, IEventPublisher events)
    {
        _jobs = jobs;
        _events = events;
    }

    public async Task<CreateJobResponse> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        var audioLanguage = string.IsNullOrWhiteSpace(request.AudioLanguage) ? "vi" : request.AudioLanguage;
        var subtitleLanguage = string.IsNullOrWhiteSpace(request.SubtitleLanguage) ? audioLanguage : request.SubtitleLanguage;
        var enableDubbing = request.EnableDubbing ?? true;

        // Persist the job first so JobSteps have a parent row and resume/tracking works from the first run.
        var job = new DubbingJob(
            sourceUrl: request.SourceUrl,
            localFilePath: null,
            sourceLanguage: null,
            audioLanguage: audioLanguage,
            subtitleLanguage: subtitleLanguage,
            enableDubbing: enableDubbing);
        await _jobs.AddAsync(job, cancellationToken);

        await _events.PublishAsync(
            new DubbingJobRequested(job.Id, request.SourceUrl, audioLanguage, subtitleLanguage, enableDubbing),
            cancellationToken);

        return new CreateJobResponse(job.Id);
    }
}
