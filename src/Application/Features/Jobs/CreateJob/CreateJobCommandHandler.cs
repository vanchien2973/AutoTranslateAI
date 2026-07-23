using Application.Interfaces;
using Application.Messaging;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Shared.Enums;

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
        var voiceGender = request.VoiceGender ?? VoiceGender.Female;
        var subtitleMode = request.SubtitleMode ?? SubtitleMode.Softsub;
        var bgmMode = request.BgmMode ?? BgmMode.DemucsAI;

        // Persist the job first so JobSteps have a parent row and resume/tracking works from the first run.
        var job = new DubbingJob(
            sourceUrl: request.SourceUrl,
            localFilePath: null,
            sourceLanguage: null,
            audioLanguage: audioLanguage,
            subtitleLanguage: subtitleLanguage,
            enableDubbing: enableDubbing,
            voiceGender: voiceGender,
            bgmMode: bgmMode,
            subtitleMode: subtitleMode);

        if (!string.IsNullOrWhiteSpace(request.LogoStorageKey))
        {
            job.SetLogo(
                request.LogoStorageKey,
                request.LogoPosition ?? Domain.Enums.LogoPosition.BottomRight,
                request.LogoScalePercent ?? 0.1,
                request.LogoMargin ?? 16);
        }

        if (request.SubtitleFontFamily is not null
            || request.SubtitleFontSize.HasValue
            || request.SubtitlePosition.HasValue
            || request.SubtitleBold.HasValue
            || request.SubtitleItalic.HasValue)
        {
            job.SetSubtitleStyle(
                request.SubtitleFontFamily,
                request.SubtitleFontSize ?? 24,
                request.SubtitlePosition ?? Domain.Enums.SubtitlePosition.Bottom,
                request.SubtitleBold ?? false,
                request.SubtitleItalic ?? false);
        }

        if (request.AutoPublishTargets is { Count: > 0 })
        {
            job.SetAutoPublishTargets(request.AutoPublishTargets.Select(target =>
                new JobPublishTarget(job.Id, target.Platform, target.ConnectionId, target.Title, target.Description)));
        }

        await _jobs.AddAsync(job, cancellationToken);

        await _events.PublishAsync(
            new DubbingJobRequested(job.Id, request.SourceUrl, audioLanguage, subtitleLanguage, enableDubbing),
            cancellationToken);

        return new CreateJobResponse(job.Id);
    }
}
