using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Data.Configurations;

public class CommunityEventReservationConfiguration :
    IEntityTypeConfiguration<CommunityEventReservation>
{
    public void Configure(
        EntityTypeBuilder<CommunityEventReservation> builder)
    {
        builder.Property(reservation => reservation.Status)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(reservation => reservation.ReservedAtUtc)
            .HasDefaultValueSql("NOW()");

        builder.Property(reservation => reservation.CancellationReason)
            .HasMaxLength(300);

        builder.HasIndex(reservation => new
            {
                reservation.CommunityEventId,
                reservation.MemberProfileId
            })
            .IsUnique();

        builder.HasOne(reservation => reservation.CommunityEvent)
            .WithMany(item => item.Reservations)
            .HasForeignKey(reservation => reservation.CommunityEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(reservation => reservation.MemberProfile)
            .WithMany(profile => profile.CommunityEventReservations)
            .HasForeignKey(reservation => reservation.MemberProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
