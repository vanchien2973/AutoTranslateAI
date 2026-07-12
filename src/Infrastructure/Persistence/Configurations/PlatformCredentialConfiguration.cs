using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class PlatformCredentialConfiguration : IEntityTypeConfiguration<PlatformCredential>
{
    public void Configure(EntityTypeBuilder<PlatformCredential> builder)
    {
        builder.ToTable("PlatformCredentials");
        builder.HasKey(credential => credential.Id);
        builder.Property(credential => credential.Id).ValueGeneratedNever();

        builder.Property(credential => credential.ClientId).HasMaxLength(256).IsRequired();
        builder.Property(credential => credential.ClientSecret).HasMaxLength(512).IsRequired();
        builder.Property(credential => credential.DefaultRedirectUri).HasMaxLength(2048);

        // Mỗi nền tảng chỉ giữ 1 bộ khóa app.
        builder.HasIndex(credential => credential.Platform).IsUnique();
    }
}
