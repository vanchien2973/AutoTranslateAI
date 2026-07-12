using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class PublishResultConfiguration : IEntityTypeConfiguration<PublishResult>
{
    public void Configure(EntityTypeBuilder<PublishResult> builder)
    {
        builder.ToTable("PublishResults");
        builder.HasKey(result => result.Id);
        builder.Property(result => result.Id).ValueGeneratedNever();

        builder.Property(result => result.ExternalId).HasMaxLength(256);
        builder.Property(result => result.Url).HasMaxLength(2048);
        builder.Property(result => result.ErrorMessage).HasMaxLength(2048);

        builder.HasIndex(result => result.JobId);
    }
}
