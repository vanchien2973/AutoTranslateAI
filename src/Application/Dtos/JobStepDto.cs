namespace Application.Dtos;

public sealed record JobStepDto(
    string StepType,
    string Status,
    int Phase,
    long? DurationMs,
    int RetryCount,
    string? ErrorMessage);
