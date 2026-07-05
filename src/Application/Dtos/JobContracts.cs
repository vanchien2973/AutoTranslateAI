namespace Application.Dtos;

public sealed record CreateJobRequest(
    string SourceUrl,
    string? AudioLanguage,
    string? SubtitleLanguage,
    bool? EnableDubbing);

public sealed record JobStatusDto(
    Guid Id,
    string Status,
    string? CurrentStep,
    int ProgressPercent,
    string? ErrorMessage,
    string? OutputUrl,
    string? DownloadUrl,
    int SegmentCount,
    int EditedSegmentCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? ReviewReadyAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<JobStepDto> Steps);

public sealed record JobStepDto(
    string StepType,
    string Status,
    int Phase,
    long? DurationMs,
    int RetryCount,
    string? ErrorMessage);
