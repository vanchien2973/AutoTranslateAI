namespace Application.Dtos;

public sealed record SystemMetricsSnapshot(
    long CpuTotalJiffies,
    long CpuIdleJiffies,
    long MemoryUsedBytes,
    long MemoryTotalBytes);
