using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<DubbingJob> DubbingJobs => Set<DubbingJob>();
    public DbSet<Segment> Segments => Set<Segment>();
    public DbSet<JobStep> JobSteps => Set<JobStep>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        BumpRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        BumpRowVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    private void BumpRowVersions()
    {
        foreach (var entry in ChangeTracker.Entries<IVersioned>())
        {
            if (entry.State == EntityState.Modified)
            {
                var rowVersion = entry.Property(e => e.RowVersion);
                rowVersion.CurrentValue += 1;
            }
        }
    }
}
