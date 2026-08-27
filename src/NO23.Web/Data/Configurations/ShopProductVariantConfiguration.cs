using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class ShopProductVariantConfiguration :
    IEntityTypeConfiguration<ShopProductVariant>
{
    public void Configure(EntityTypeBuilder<ShopProductVariant> builder)
    {
        builder.Property(variant => variant.Size)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(variant => variant.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(variant => new
            {
                variant.ShopProductId,
                variant.Size
            })
            .IsUnique();

        builder.HasOne(variant => variant.ShopProduct)
            .WithMany(product => product.Variants)
            .HasForeignKey(variant => variant.ShopProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
