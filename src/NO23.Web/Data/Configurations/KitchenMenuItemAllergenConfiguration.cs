using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenMenuItemAllergenConfiguration : IEntityTypeConfiguration<KitchenMenuItemAllergen>
{
    public void Configure(EntityTypeBuilder<KitchenMenuItemAllergen> builder)
    {
        builder.HasKey(item => new { item.KitchenMenuItemId, item.KitchenAllergenId });
        builder.HasOne(item => item.KitchenMenuItem).WithMany(item => item.MenuItemAllergens)
            .HasForeignKey(item => item.KitchenMenuItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.KitchenAllergen).WithMany(item => item.MenuItems)
            .HasForeignKey(item => item.KitchenAllergenId).OnDelete(DeleteBehavior.Restrict);
    }
}
