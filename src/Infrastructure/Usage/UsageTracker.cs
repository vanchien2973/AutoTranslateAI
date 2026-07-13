using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Configuration;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Usage;

public sealed class UsageTracker : IUsageTracker
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly UsagePricing _pricing;
    private readonly ILogger<UsageTracker> _logger;

    public UsageTracker(
        IDbContextFactory<AppDbContext> contextFactory,
        IOptions<PricingOptions> pricing,
        ILogger<UsageTracker> logger)
    {
        _contextFactory = contextFactory;
        _pricing = pricing.Value.ToPricing();
        _logger = logger;
    }

    public async Task RecordAsync(UsageEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var cost = CostCalculator.Estimate(entry.Unit, entry.InputUnits, entry.OutputUnits, _pricing);
            var record = new UsageRecord(
                entry.Provider, entry.Operation, entry.Unit, entry.InputUnits, entry.OutputUnits, cost, entry.JobId);

            await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
            await db.UsageRecords.AddAsync(record, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to record usage for {Provider}/{Operation}", entry.Provider, entry.Operation);
        }
    }
}
