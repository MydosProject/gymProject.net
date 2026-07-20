using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.HasIndex(cart => cart.MemberProfileId)
            .IsUnique();

        builder.Property(cart => cart.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasOne(cart => cart.MemberProfile)
            .WithOne(member => member.ShoppingCart)
            .HasForeignKey<ShoppingCart>(cart => cart.MemberProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
