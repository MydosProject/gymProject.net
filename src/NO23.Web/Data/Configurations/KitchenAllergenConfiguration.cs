using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenAllergenConfiguration : IEntityTypeConfiguration<KitchenAllergen>
{
    public void Configure(EntityTypeBuilder<KitchenAllergen> builder)
    {
        builder.Property(item => item.Name).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(500);
        builder.Property(item => item.CreatedAtUtc).HasDefaultValueSql("NOW()");
        builder.HasIndex(item => item.Name).IsUnique();
    }
}
