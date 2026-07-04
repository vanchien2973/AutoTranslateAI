namespace Infrastructure.Configuration;

public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";
    public int MaxRetryAttempts { get; init; } = 3;
    public int BaseDelaySeconds { get; init; } = 2;
    public int PerAttemptTimeoutSeconds { get; init; } = 100;
}
