namespace Application.Dtos;

public sealed record JobSummaryDto(
    Guid Id,
    string Status,
    string? SourceUrl,
    string? CurrentStep,
    int ProgressPercent,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? ReviewReadyAt,
    DateTimeOffset? CompletedAt);
