using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenMenuItemConfiguration : IEntityTypeConfiguration<KitchenMenuItem>
{
    public void Configure(EntityTypeBuilder<KitchenMenuItem> builder)
    {
        builder.Property(item => item.Name)
            .HasMaxLength(140)
            .IsRequired();

        builder.Property(item => item.Description)
            .HasMaxLength(700);

        builder.Property(item => item.Category)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(item => item.UnitPrice)
            .HasPrecision(10, 2);

        builder.Property(item => item.ProteinGrams)
            .HasPrecision(6, 2);

        builder.Property(item => item.CarbohydrateGrams)
            .HasPrecision(6, 2);

        builder.Property(item => item.FatGrams)
            .HasPrecision(6, 2);

        builder.Property(item => item.Ingredients)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(item => item.Tags)
            .HasMaxLength(500);

        builder.Property(item => item.IsPlanEligible)
            .HasDefaultValue(true);

        builder.Property(item => item.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");
    }
}
