using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class KitchenSubscriptionConfiguration : IEntityTypeConfiguration<KitchenSubscription>
{
    public void Configure(EntityTypeBuilder<KitchenSubscription> builder)
    {
        builder.Property(subscription => subscription.Plan)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(subscription => subscription.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(subscription => subscription.Goal)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(subscription => subscription.PackageNameSnapshot)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(subscription => subscription.PackagePriceSnapshot)
            .HasPrecision(10, 2);

        builder.HasIndex(subscription => new { subscription.MemberProfileId, subscription.Status });

        builder.Property(subscription => subscription.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasOne(subscription => subscription.MemberProfile)
            .WithMany(profile => profile.KitchenSubscriptions)
            .HasForeignKey(subscription => subscription.MemberProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(subscription => subscription.KitchenSubscriptionPackage)
            .WithMany(package => package.KitchenSubscriptions)
            .HasForeignKey(subscription => subscription.KitchenSubscriptionPackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
