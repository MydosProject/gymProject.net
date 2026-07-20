using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class ShopProductConfiguration : IEntityTypeConfiguration<ShopProduct>
{
    public void Configure(EntityTypeBuilder<ShopProduct> builder)
    {
        builder.Property(product => product.Name)
            .HasMaxLength(140)
            .IsRequired();

        builder.Property(product => product.Sku)
            .HasMaxLength(60)
            .IsRequired();

        builder.HasIndex(product => product.Sku)
            .IsUnique();

        builder.Property(product => product.Description)
            .HasMaxLength(700);

        builder.Property(product => product.Category)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(product => product.UnitPrice)
            .HasPrecision(10, 2);

        builder.Property(product => product.ImageUrl)
            .HasMaxLength(500);

        builder.Property(product => product.Tags)
            .HasMaxLength(500);

        builder.Property(product => product.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");
    }
}
