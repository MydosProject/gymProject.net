using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Data.Configurations;

public class MembershipPackageChangeRequestConfiguration
    : IEntityTypeConfiguration<MembershipPackageChangeRequest>
{
    public void Configure(
        EntityTypeBuilder<MembershipPackageChangeRequest> builder)
    {
        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .HasDefaultValue(MembershipPackageChangeRequestStatus.Pending);

        builder.Property(request => request.RequestedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.Property(request => request.ResolvedByUserId)
            .HasMaxLength(450);

        builder.Property(request => request.AdminNote)
            .HasMaxLength(1200);

        builder.HasIndex(request => request.Status);

        builder.HasIndex(request => request.RequestedAtUtc);

        builder.HasIndex(request => new
        {
            request.MemberProfileId,
            request.Status
        });

        builder.HasIndex(request => request.MemberProfileId)
            .IsUnique()
            .HasDatabaseName(
                "IX_MembershipPackageChangeRequests_MemberProfileId_Pending")
            .HasFilter("\"Status\" = 'Pending'");

        builder.HasOne(request => request.MemberProfile)
            .WithMany(profile => profile.MembershipPackageChangeRequests)
            .HasForeignKey(request => request.MemberProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(request => request.CurrentMembershipPackage)
            .WithMany()
            .HasForeignKey(request => request.CurrentMembershipPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.RequestedMembershipPackage)
            .WithMany()
            .HasForeignKey(request => request.RequestedMembershipPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.ResolvedByUser)
            .WithMany()
            .HasForeignKey(request => request.ResolvedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
