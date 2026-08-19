using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Configurations;

public class MemberProfileConfiguration : IEntityTypeConfiguration<MemberProfile>
{
    public void Configure(EntityTypeBuilder<MemberProfile> builder)
    {
        builder.HasIndex(profile => profile.ApplicationUserId)
            .IsUnique();

        builder.Property(profile => profile.ApplicationUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(profile => profile.FitnessGoal)
            .HasMaxLength(160);

        builder.Property(profile => profile.MembershipStartsAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.Property(profile => profile.MembershipEndsAtUtc)
            .HasDefaultValueSql(
                $"NOW() + INTERVAL '{MemberProfile.DefaultMembershipDurationDays} days'");

        builder.Property(profile => profile.MembershipStatus)
            .HasConversion<string>()
            .HasMaxLength(40)
            .HasDefaultValue(MembershipStatus.Active);

        builder.Property(profile => profile.MembershipCancellationReason)
            .HasMaxLength(240);

        builder.Property(profile => profile.IyzicoCustomerReferenceCode)
            .HasMaxLength(120);

        builder.Property(profile => profile.IyzicoSubscriptionReferenceCode)
            .HasMaxLength(120);

        builder.Property(profile => profile.IyzicoPricingPlanReferenceCode)
            .HasMaxLength(120);

        builder.Property(profile => profile.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasOne(profile => profile.ApplicationUser)
            .WithOne(user => user.MemberProfile)
            .HasForeignKey<MemberProfile>(profile => profile.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(profile => profile.MembershipPackage)
            .WithMany(package => package.MemberProfiles)
            .HasForeignKey(profile => profile.MembershipPackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
