using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class DubbingJobConfiguration : IEntityTypeConfiguration<DubbingJob>
{
    public void Configure(EntityTypeBuilder<DubbingJob> builder)
    {
        builder.ToTable("DubbingJobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id).ValueGeneratedNever();
        builder.Property(j => j.RowVersion).IsConcurrencyToken();

        builder.Property(j => j.SourceLanguage).HasMaxLength(10);
        builder.Property(j => j.AudioLanguage).HasMaxLength(10).IsRequired();
        builder.Property(j => j.SubtitleLanguage).HasMaxLength(10);

        builder.HasIndex(j => j.Status);

        builder.HasMany(j => j.Segments)
            .WithOne()
            .HasForeignKey(s => s.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(j => j.Steps)
            .WithOne()
            .HasForeignKey(s => s.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(j => j.Segments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(j => j.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
