using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class JobStepConfiguration : IEntityTypeConfiguration<JobStep>
{
    public void Configure(EntityTypeBuilder<JobStep> builder)
    {
        builder.ToTable("JobSteps");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.OutputPath);
        builder.Property(s => s.ErrorMessage);

        builder.HasIndex(s => new { s.JobId, s.StepType }).IsUnique();
    }
}
