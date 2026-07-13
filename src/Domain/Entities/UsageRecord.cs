using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public sealed class UsageRecord : BaseEntity
{
    private UsageRecord()
    {
    }

    public UsageRecord(
        string provider,
        string operation,
        UsageUnit unit,
        long inputUnits,
        long outputUnits,
        decimal estimatedCostUsd,
        Guid? jobId = null)
    {
        Provider = provider;
        Operation = operation;
        Unit = unit;
        InputUnits = inputUnits;
        OutputUnits = outputUnits;
        EstimatedCostUsd = estimatedCostUsd;
        JobId = jobId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Provider { get; private set; } = string.Empty;
    public string Operation { get; private set; } = string.Empty;
    public UsageUnit Unit { get; private set; }
    public long InputUnits { get; private set; }
    public long OutputUnits { get; private set; }
    public decimal EstimatedCostUsd { get; private set; }
    public Guid? JobId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
