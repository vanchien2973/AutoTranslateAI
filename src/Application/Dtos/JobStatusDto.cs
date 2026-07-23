using Domain.Enums;

namespace Application.Dtos;

public sealed record JobStatusDto(
    Guid Id,
    string Status,
    string AudioLanguage,
    string? SubtitleLanguage,
    bool EnableDubbing,
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
    IReadOnlyList<JobStepDto> Steps,
    SubtitleMode SubtitleMode,
    string? SubtitleFontFamily,
    int SubtitleFontSize,
    SubtitlePosition SubtitlePosition,
    bool SubtitleBold,
    bool SubtitleItalic);
