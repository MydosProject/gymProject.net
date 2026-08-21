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

        builder.Property(profile => profile.FitnessGoal)
            .HasMaxLength(160);

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

        builder.HasOne(profile => profile.AssignedTrainer)
            .WithMany(trainer => trainer.AssignedMembers)
            .HasForeignKey(profile => profile.AssignedTrainerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
