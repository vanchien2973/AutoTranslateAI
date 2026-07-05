using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SegmentConfiguration : IEntityTypeConfiguration<Segment>
{
    public void Configure(EntityTypeBuilder<Segment> builder)
    {
        builder.ToTable("Segments");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.RowVersion).IsConcurrencyToken();

        builder.Property(s => s.OriginalText).IsRequired();
        builder.Property(s => s.SpeakerLabel).HasMaxLength(50);
        builder.Property(s => s.AssignedVoice).HasMaxLength(100);

        builder.Ignore(s => s.TtsText);
        builder.Ignore(s => s.SubtitleText);
        builder.Ignore(s => s.Duration);

        builder.HasIndex(s => new { s.JobId, s.SegmentIndex }).IsUnique();
    }
}
