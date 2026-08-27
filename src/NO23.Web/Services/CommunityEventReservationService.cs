using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class CommunityEventReservationService(
    ApplicationDbContext dbContext)
{
    public async Task<CommunityEventReservationResult> ReserveAsync(
        string userId,
        string slug)
    {
        var member = await dbContext.MemberProfiles
            .FirstOrDefaultAsync(profile =>
                profile.ApplicationUserId == userId);

        if (member is null)
        {
            return CommunityEventReservationResult.Fail(
                "Üye profili bulunamadı.");
        }

        var eventItem = await dbContext.CommunityEvents
            .Include(item => item.Reservations)
            .FirstOrDefaultAsync(item => item.Slug == slug);

        if (eventItem is null)
        {
            return CommunityEventReservationResult.Fail(
                "Etkinlik bulunamadı.");
        }

        if (!CommunityEventLifecycle.IsReservationOpen(
                eventItem.Status,
                eventItem.StartsAtUtc,
                eventItem.EndsAtUtc,
                DateTime.UtcNow))
        {
            return CommunityEventReservationResult.Fail(
                "Bu etkinlik için rezervasyon kapalı.");
        }

        var existingReservation = eventItem.Reservations
            .FirstOrDefault(reservation =>
                reservation.MemberProfileId == member.Id);

        if (existingReservation?.Status ==
            CommunityEventReservationStatus.Reserved)
        {
            return CommunityEventReservationResult.Fail(
                "Bu etkinliğe zaten rezervasyon yaptın.");
        }

        var reservedCount = eventItem.Reservations.Count(reservation =>
            reservation.Status == CommunityEventReservationStatus.Reserved);

        if (eventItem.Capacity.HasValue &&
            reservedCount >= eventItem.Capacity.Value)
        {
            return CommunityEventReservationResult.Fail(
                "Etkinlik kontenjanı dolu.");
        }

        var nowUtc = DateTime.UtcNow;

        if (existingReservation is null)
        {
            eventItem.Reservations.Add(new CommunityEventReservation
            {
                MemberProfileId = member.Id,
                Status = CommunityEventReservationStatus.Reserved,
                ReservedAtUtc = nowUtc
            });
        }
        else
        {
            existingReservation.Status =
                CommunityEventReservationStatus.Reserved;
            existingReservation.ReservedAtUtc = nowUtc;
            existingReservation.CancelledAtUtc = null;
            existingReservation.CancellationReason = null;
        }

        await dbContext.SaveChangesAsync();
        return CommunityEventReservationResult.Ok(
            "Etkinlik rezervasyonun oluşturuldu.");
    }

    public async Task<CommunityEventReservationResult> CancelAsync(
        string userId,
        string slug)
    {
        var reservation = await dbContext.CommunityEventReservations
            .Include(item => item.CommunityEvent)
            .FirstOrDefaultAsync(item =>
                item.CommunityEvent.Slug == slug &&
                item.MemberProfile.ApplicationUserId == userId);

        if (reservation is null ||
            reservation.Status != CommunityEventReservationStatus.Reserved)
        {
            return CommunityEventReservationResult.Fail(
                "Aktif etkinlik rezervasyonu bulunamadı.");
        }

        if (reservation.CommunityEvent.StartsAtUtc <= DateTime.UtcNow)
        {
            return CommunityEventReservationResult.Fail(
                "Başlamış etkinliğin rezervasyonu iptal edilemez.");
        }

        reservation.Status = CommunityEventReservationStatus.Cancelled;
        reservation.CancelledAtUtc = DateTime.UtcNow;
        reservation.CancellationReason = "Üye tarafından iptal edildi.";

        await dbContext.SaveChangesAsync();
        return CommunityEventReservationResult.Ok(
            "Etkinlik rezervasyonun iptal edildi.");
    }
}

public record CommunityEventReservationResult(
    bool Succeeded,
    string Message)
{
    public static CommunityEventReservationResult Ok(string message) =>
        new(true, message);

    public static CommunityEventReservationResult Fail(string message) =>
        new(false, message);
}
