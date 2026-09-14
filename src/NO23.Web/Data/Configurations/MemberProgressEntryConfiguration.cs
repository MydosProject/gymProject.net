using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class MemberProgressEntryConfiguration : IEntityTypeConfiguration<MemberProgressEntry>
{
    public void Configure(EntityTypeBuilder<MemberProgressEntry> builder)
    {
        builder.Property(entry => entry.BodyWeightKg).HasPrecision(7, 2);
        builder.Property(entry => entry.HeightCm).HasPrecision(6, 2);
        builder.Property(entry => entry.ShoulderCm).HasPrecision(6, 2);
        builder.Property(entry => entry.ChestCm).HasPrecision(6, 2);
        builder.Property(entry => entry.RightArmCm).HasPrecision(6, 2);
        builder.Property(entry => entry.LeftArmCm).HasPrecision(6, 2);
        builder.Property(entry => entry.WaistCm).HasPrecision(6, 2);
        builder.Property(entry => entry.AbdomenCm).HasPrecision(6, 2);
        builder.Property(entry => entry.HipCm).HasPrecision(6, 2);
        builder.Property(entry => entry.RightUpperLegCm).HasPrecision(6, 2);
        builder.Property(entry => entry.LeftUpperLegCm).HasPrecision(6, 2);
        builder.Property(entry => entry.BodyFatKg).HasPrecision(7, 2);
        builder.Property(entry => entry.BodyFatPercent).HasPrecision(5, 2);
        builder.Property(entry => entry.MuscleMassKg).HasPrecision(7, 2);
        builder.Property(entry => entry.MuscleMassPercent).HasPrecision(5, 2);
        builder.Property(entry => entry.BodyWaterAmount).HasPrecision(7, 2);
        builder.Property(entry => entry.BodyWaterPercent).HasPrecision(5, 2);
        builder.Property(entry => entry.DailyWaterIntakeLiters).HasPrecision(5, 2);

        builder.Property(entry => entry.CreatedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(entry => new
            {
                entry.MemberProfileId,
                entry.EntryDate
            })
            .IsUnique();

        builder.HasOne(entry => entry.MemberProfile)
            .WithMany(profile => profile.ProgressEntries)
            .HasForeignKey(entry => entry.MemberProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
