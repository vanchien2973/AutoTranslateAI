namespace Infrastructure.Configuration;

public sealed class CleanupOptions
{
    public const string SectionName = "Cleanup";

    public bool Enabled { get; init; } = true;
    public int RunIntervalHours { get; init; } = 6;
    public int JobRetentionDays { get; init; } = 14;
    public long MaxWorkspaceBytes { get; init; } = 20L * 1024 * 1024 * 1024; // 20 GB
}
