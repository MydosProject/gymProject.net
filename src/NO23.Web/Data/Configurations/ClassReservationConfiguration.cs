using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class ClassReservationConfiguration : IEntityTypeConfiguration<ClassReservation>
{
    public void Configure(EntityTypeBuilder<ClassReservation> builder)
    {
        builder.Property(reservation => reservation.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(reservation => reservation.CancellationReason)
            .HasMaxLength(300);

        builder.Property(reservation => reservation.ReservedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(reservation => new { reservation.ClassSessionId, reservation.MemberProfileId })
            .IsUnique();

        builder.HasOne(reservation => reservation.ClassSession)
            .WithMany(session => session.Reservations)
            .HasForeignKey(reservation => reservation.ClassSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(reservation => reservation.MemberProfile)
            .WithMany(profile => profile.ClassReservations)
            .HasForeignKey(reservation => reservation.MemberProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
