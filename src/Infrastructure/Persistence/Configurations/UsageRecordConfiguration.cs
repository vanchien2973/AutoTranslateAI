using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class UsageRecordConfiguration : IEntityTypeConfiguration<UsageRecord>
{
    public void Configure(EntityTypeBuilder<UsageRecord> builder)
    {
        builder.ToTable("UsageRecords");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever();

        builder.Property(record => record.Provider).HasMaxLength(64).IsRequired();
        builder.Property(record => record.Operation).HasMaxLength(64).IsRequired();
        builder.Property(record => record.EstimatedCostUsd).HasPrecision(18, 6);

        builder.HasIndex(record => record.CreatedAt);
        builder.HasIndex(record => record.JobId);
    }
}
