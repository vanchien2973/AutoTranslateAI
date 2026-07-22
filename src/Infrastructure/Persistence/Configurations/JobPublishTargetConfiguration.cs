using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class JobPublishTargetConfiguration : IEntityTypeConfiguration<JobPublishTarget>
{
    public void Configure(EntityTypeBuilder<JobPublishTarget> builder)
    {
        builder.ToTable("JobPublishTargets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Title).HasMaxLength(JobPublishTarget.MaxTitleLength);
        builder.Property(t => t.Description).HasMaxLength(5000);

        builder.HasIndex(t => new { t.JobId, t.Platform }).IsUnique();
    }
}
