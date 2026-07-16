namespace Application.Messaging;

public sealed record JobMetricsUpdated(Guid JobId, double CpuPercent, long MemoryUsedBytes, long MemoryTotalBytes);
