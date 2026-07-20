using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(item => item.ItemType)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(item => item.ProductName)
            .HasMaxLength(140)
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasPrecision(10, 2);

        builder.Property(item => item.LineTotal)
            .HasPrecision(10, 2);

        builder.HasOne(item => item.Order)
            .WithMany(order => order.Items)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.KitchenMenuItem)
            .WithMany(menuItem => menuItem.OrderItems)
            .HasForeignKey(item => item.KitchenMenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.ShopProduct)
            .WithMany(product => product.OrderItems)
            .HasForeignKey(item => item.ShopProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
