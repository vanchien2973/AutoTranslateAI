namespace Application.Dtos;

public sealed record UsageSummary(
    decimal TotalCostUsd,
    int CallCount,
    IReadOnlyList<UsageByKey> ByProvider,
    IReadOnlyList<UsageByKey> ByOperation,
    IReadOnlyList<UsageByDay> ByDay);

public sealed record UsageByKey(string Key, decimal CostUsd, int CallCount, long InputUnits, long OutputUnits);

public sealed record UsageByDay(DateOnly Date, decimal CostUsd, int CallCount);
