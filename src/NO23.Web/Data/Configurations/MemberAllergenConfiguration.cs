using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class MemberAllergenConfiguration : IEntityTypeConfiguration<MemberAllergen>
{
    public void Configure(EntityTypeBuilder<MemberAllergen> builder)
    {
        builder.HasKey(item => new { item.MemberProfileId, item.KitchenAllergenId });
        builder.Property(item => item.CreatedAtUtc).HasDefaultValueSql("NOW()");
        builder.HasOne(item => item.MemberProfile).WithMany(item => item.Allergens)
            .HasForeignKey(item => item.MemberProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.KitchenAllergen).WithMany(item => item.Members)
            .HasForeignKey(item => item.KitchenAllergenId).OnDelete(DeleteBehavior.Restrict);
    }
}
