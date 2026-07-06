using MediatR;

namespace Application.Features.Jobs.CreateJob;

public sealed record CreateJobCommand(
    string SourceUrl,
    string? AudioLanguage,
    string? SubtitleLanguage,
    bool? EnableDubbing) : IRequest<CreateJobResponse>;
