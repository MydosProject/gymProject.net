using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.Property(item => item.ItemType)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(item => item.ProductName)
            .HasMaxLength(140)
            .IsRequired();

        builder.Property(item => item.SelectedSize)
            .HasMaxLength(50);

        builder.Property(item => item.RemovedIngredientNames)
            .HasMaxLength(3000);

        builder.Property(item => item.AddedIngredientNames)
            .HasMaxLength(3000);

        builder.Property(item => item.UnitPrice)
            .HasPrecision(10, 2);

        builder.Ignore(item => item.LineTotal);

        builder.Property(item => item.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasOne(item => item.ShoppingCart)
            .WithMany(cart => cart.Items)
            .HasForeignKey(item => item.ShoppingCartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.KitchenMenuItem)
            .WithMany(menuItem => menuItem.CartItems)
            .HasForeignKey(item => item.KitchenMenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.ShopProduct)
            .WithMany(product => product.CartItems)
            .HasForeignKey(item => item.ShopProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.ShopProductVariant)
            .WithMany(variant => variant.CartItems)
            .HasForeignKey(item => item.ShopProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
