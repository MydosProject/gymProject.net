using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenSubscriptionPackageConfiguration : IEntityTypeConfiguration<KitchenSubscriptionPackage>
{
    public void Configure(EntityTypeBuilder<KitchenSubscriptionPackage> builder)
    {
        builder.Property(package => package.Plan)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.HasIndex(package => package.Plan)
            .IsUnique();

        builder.Property(package => package.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(package => package.Description)
            .HasMaxLength(600)
            .IsRequired();

        builder.Property(package => package.UnitPrice)
            .HasPrecision(10, 2);

        builder.Property(package => package.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");
    }
}
