using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class CommunityChallengeParticipationConfiguration
    : IEntityTypeConfiguration<CommunityChallengeParticipation>
{
    public void Configure(EntityTypeBuilder<CommunityChallengeParticipation> builder)
    {
        builder.Property(participation => participation.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(participation => participation.JoinedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(participation => new
            {
                participation.CommunityChallengeId,
                participation.MemberProfileId
            })
            .IsUnique();

        builder.HasOne(participation => participation.CommunityChallenge)
            .WithMany(challenge => challenge.Participations)
            .HasForeignKey(participation => participation.CommunityChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(participation => participation.MemberProfile)
            .WithMany(profile => profile.CommunityChallengeParticipations)
            .HasForeignKey(participation => participation.MemberProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
