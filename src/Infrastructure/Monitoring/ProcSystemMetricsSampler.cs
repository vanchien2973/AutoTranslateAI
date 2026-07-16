using Application.Interfaces;

namespace Infrastructure.Monitoring;

public sealed class ProcSystemMetricsSampler : ISystemMetricsSampler
{
    private const string StatPath = "/proc/stat";
    private const string MemInfoPath = "/proc/meminfo";

    public async ValueTask<SystemMetricsSnapshot> SampleAsync(CancellationToken cancellationToken)
    {
        var (total, idle) = await ReadCpuAsync(cancellationToken);
        var (used, memTotal) = await ReadMemoryAsync(cancellationToken);
        return new SystemMetricsSnapshot(total, idle, used, memTotal);
    }

    private static async ValueTask<(long Total, long Idle)> ReadCpuAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(StatPath))
        {
            return (0, 0);
        }

        foreach (var line in await File.ReadAllLinesAsync(StatPath, cancellationToken))
        {
            if (!line.StartsWith("cpu ", StringComparison.Ordinal))
            {
                continue;
            }

            // Fields: user nice system idle iowait irq softirq steal ... (index 1..). idle = idle + iowait.
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            long total = 0;
            long idle = 0;
            for (var i = 1; i < parts.Length; i++)
            {
                if (!long.TryParse(parts[i], out var value))
                {
                    continue;
                }

                total += value;
                if (i is 4 or 5)
                {
                    idle += value;
                }
            }

            return (total, idle);
        }

        return (0, 0);
    }

    private static async ValueTask<(long Used, long Total)> ReadMemoryAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(MemInfoPath))
        {
            return (0, 0);
        }

        long total = 0;
        long available = 0;
        foreach (var line in await File.ReadAllLinesAsync(MemInfoPath, cancellationToken))
        {
            if (line.StartsWith("MemTotal:", StringComparison.Ordinal))
            {
                total = ParseKilobytes(line);
            }
            else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
            {
                available = ParseKilobytes(line);
            }

            if (total > 0 && available > 0)
            {
                break;
            }
        }

        return (Math.Max(0, total - available), total);
    }

    private static long ParseKilobytes(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && long.TryParse(parts[1], out var kb) ? kb * 1024 : 0;
    }
}
