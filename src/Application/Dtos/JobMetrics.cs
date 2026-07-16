namespace Application.Dtos;

public sealed record JobMetrics(Guid JobId, double CpuPercent, long MemoryUsedBytes, long MemoryTotalBytes);
