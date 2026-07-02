namespace Application.Messaging;

public sealed record DubbingJobRequested(
    Guid JobId,
    string SourceUrl,
    string AudioLanguage,
    string SubtitleLanguage,
    bool EnableDubbing);
