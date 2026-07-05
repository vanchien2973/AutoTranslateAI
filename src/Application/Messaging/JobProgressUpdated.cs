namespace Application.Messaging;

public sealed record JobProgressUpdated(Guid JobId, string Status, string? CurrentStep, int ProgressPercent);
