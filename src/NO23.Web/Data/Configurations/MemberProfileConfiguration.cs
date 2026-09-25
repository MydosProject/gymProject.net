using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

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

        builder.Property(profile => profile.NationalIdentityNumber)
            .HasMaxLength(11);

        builder.HasIndex(profile => profile.NationalIdentityNumber)
            .IsUnique()
            .HasFilter("\"NationalIdentityNumber\" IS NOT NULL");

        builder.Property(profile => profile.FitnessGoal)
            .HasMaxLength(160);

        builder.Property(profile => profile.FamilyCode)
            .HasMaxLength(32);

        builder.HasIndex(profile => profile.FamilyCode);

        builder.Property(profile => profile.ReferralCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(profile => profile.ReferralCode)
            .IsUnique();

        builder.HasOne(profile => profile.ReferredByMemberProfile)
            .WithMany(profile => profile.ReferredMembers)
            .HasForeignKey(profile => profile.ReferredByMemberProfileId)
            .OnDelete(DeleteBehavior.Restrict);

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

        builder.HasOne(profile => profile.MembershipPackageOption)
            .WithMany(option => option.MemberProfiles)
            .HasForeignKey(profile => profile.MembershipPackageOptionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(profile => profile.ServicePackageVariant)
            .WithMany(variant => variant.MemberProfiles)
            .HasForeignKey(profile => profile.ServicePackageVariantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(profile => profile.AssignedTrainer)
            .WithMany(trainer => trainer.AssignedMembers)
            .HasForeignKey(profile => profile.AssignedTrainerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
