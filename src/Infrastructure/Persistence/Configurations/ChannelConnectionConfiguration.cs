using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ChannelConnectionConfiguration : IEntityTypeConfiguration<ChannelConnection>
{
    public void Configure(EntityTypeBuilder<ChannelConnection> builder)
    {
        builder.ToTable("ChannelConnections");
        builder.HasKey(connection => connection.Id);
        builder.Property(connection => connection.Id).ValueGeneratedNever();

        builder.Property(connection => connection.ChannelId).HasMaxLength(256).IsRequired();
        builder.Property(connection => connection.ChannelName).HasMaxLength(256).IsRequired();
        builder.Property(connection => connection.AccessToken).HasMaxLength(4096).IsRequired();
        builder.Property(connection => connection.RefreshToken).HasMaxLength(4096);

        builder.HasIndex(connection => new { connection.Platform, connection.CreatedAt });
    }
}
