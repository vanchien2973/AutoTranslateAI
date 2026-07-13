using Domain.Entities;

namespace Application.Helpers;

public static class UsageSummarizer
{
    public static UsageSummary Summarize(IReadOnlyList<UsageRecord> records)
    {
        var byProvider = records
            .GroupBy(record => record.Provider)
            .Select(ToKey)
            .OrderByDescending(entry => entry.CostUsd)
            .ToList();

        var byOperation = records
            .GroupBy(record => record.Operation)
            .Select(ToKey)
            .OrderByDescending(entry => entry.CostUsd)
            .ToList();

        var byDay = records
            .GroupBy(record => DateOnly.FromDateTime(record.CreatedAt.UtcDateTime))
            .Select(group => new UsageByDay(group.Key, group.Sum(record => record.EstimatedCostUsd), group.Count()))
            .OrderBy(entry => entry.Date)
            .ToList();

        return new UsageSummary(
            records.Sum(record => record.EstimatedCostUsd),
            records.Count,
            byProvider,
            byOperation,
            byDay);
    }

    private static UsageByKey ToKey(IGrouping<string, UsageRecord> group) =>
        new(
            group.Key,
            group.Sum(record => record.EstimatedCostUsd),
            group.Count(),
            group.Sum(record => record.InputUnits),
            group.Sum(record => record.OutputUnits));
}
