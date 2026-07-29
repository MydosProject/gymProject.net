using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class ChallengeProgressEntryConfiguration : IEntityTypeConfiguration<ChallengeProgressEntry>
{
    public void Configure(EntityTypeBuilder<ChallengeProgressEntry> builder)
    {
        builder.Property(entry => entry.CalorieTolerancePercentSnapshot)
            .HasPrecision(5, 2);

        builder.Property(entry => entry.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(entry => new
            {
                entry.CommunityChallengeParticipationId,
                entry.EntryDate
            })
            .IsUnique();

        builder.HasOne(entry => entry.CommunityChallengeParticipation)
            .WithMany(participation => participation.ProgressEntries)
            .HasForeignKey(entry => entry.CommunityChallengeParticipationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
