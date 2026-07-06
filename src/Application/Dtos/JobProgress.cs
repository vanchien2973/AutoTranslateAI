namespace Application.Dtos;

public sealed record JobProgress(Guid JobId, string Status, string? CurrentStep, int ProgressPercent);
