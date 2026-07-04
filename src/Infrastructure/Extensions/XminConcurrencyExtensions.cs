using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Microsoft.EntityFrameworkCore;

internal static class XminConcurrencyExtensions
{
    public static EntityTypeBuilder<TEntity> UseXminAsConcurrencyToken<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        return builder;
    }
}
