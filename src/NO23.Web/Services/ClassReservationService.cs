using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class ClassReservationService(ApplicationDbContext dbContext)
{
    public static readonly TimeSpan CancellationWindow = TimeSpan.FromHours(2);

    public async Task<ReservationResult> ReserveAsync(string userId, int classSessionId)
    {
        var profile = await dbContext.MemberProfiles
            .Include(member => member.MembershipPackage)
            .FirstOrDefaultAsync(member => member.ApplicationUserId == userId);

        if (profile is null)
        {
            return ReservationResult.Fail("Üye profili bulunamadı.");
        }

        if (profile.IsSuspended)
        {
            return ReservationResult.Fail(
                "Üyeliğiniz askıya alındığı için ders rezervasyonu yapamazsınız.");
        }

        var session = await dbContext.ClassSessions
            .Include(classSession => classSession.GroupClass)
            .Include(classSession => classSession.Reservations)
            .FirstOrDefaultAsync(classSession => classSession.Id == classSessionId);

        if (session is null)
        {
            return ReservationResult.Fail("Ders programı bulunamadı.");
        }

        if (!MemberMembershipService.CanReserveClassSession(
                profile,
                session.StartsAtUtc,
                DateTime.UtcNow,
                out var membershipErrorMessage))
        {
            return ReservationResult.Fail(membershipErrorMessage);
        }

        if (!ClassSessionLifecycle.IsReservationOpen(
                session.Status,
                session.StartsAtUtc,
                DateTime.UtcNow,
                session.GroupClass.IsActive))
        {
            return ReservationResult.Fail("Bu ders için rezervasyon alınamaz.");
        }

        var existingReservation = await dbContext.ClassReservations
            .FirstOrDefaultAsync(reservation =>
                reservation.ClassSessionId == classSessionId &&
                reservation.MemberProfileId == profile.Id);

        if (existingReservation?.Status == ClassReservationStatus.Reserved)
        {
            return ReservationResult.Fail("Bu derse zaten rezervasyonun var.");
        }

        var activeReservationCount = session.Reservations.Count(reservation =>
            reservation.Status == ClassReservationStatus.Reserved);

        var capacity = session.CapacityOverride ?? session.GroupClass.Capacity;

        if (activeReservationCount >= capacity)
        {
            return ReservationResult.Fail("Ders kontenjanı dolu.");
        }

        var isUnlimitedPackage = profile.MembershipPackage.WeeklyClassLimit is null;

        if (!isUnlimitedPackage && profile.RemainingClassCredits <= 0)
        {
            return ReservationResult.Fail("Kalan ders hakkın bulunmuyor.");
        }

        if (existingReservation is null)
        {
            dbContext.ClassReservations.Add(new ClassReservation
            {
                ClassSessionId = classSessionId,
                MemberProfileId = profile.Id
            });
        }
        else
        {
            existingReservation.Status = ClassReservationStatus.Reserved;
            existingReservation.ReservedAtUtc = DateTime.UtcNow;
            existingReservation.CancelledAtUtc = null;
            existingReservation.CancellationReason = null;
        }

        if (!isUnlimitedPackage)
        {
            profile.RemainingClassCredits--;
        }

        profile.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return ReservationResult.Ok();
    }

    public async Task<ReservationResult> CancelAsync(string userId, int reservationId)
    {
        var reservation = await dbContext.ClassReservations
            .Include(item => item.MemberProfile)
            .ThenInclude(profile => profile.MembershipPackage)
            .Include(item => item.ClassSession)
            .FirstOrDefaultAsync(item =>
                item.Id == reservationId &&
                item.MemberProfile.ApplicationUserId == userId);

        if (reservation is null)
        {
            return ReservationResult.Fail("Rezervasyon bulunamadı.");
        }

        if (reservation.Status != ClassReservationStatus.Reserved)
        {
            return ReservationResult.Fail("Bu rezervasyon zaten aktif değil.");
        }

        if (reservation.ClassSession.StartsAtUtc - DateTime.UtcNow < CancellationWindow)
        {
            return ReservationResult.Fail(
                "Rezervasyon iptali ders başlangıcından 2 saat öncesine kadar yapılabilir.");
        }

        reservation.Status = ClassReservationStatus.Cancelled;
        reservation.CancelledAtUtc = DateTime.UtcNow;
        reservation.CancellationReason = "Member cancellation";

        if (reservation.MemberProfile.MembershipPackage.WeeklyClassLimit is not null)
        {
            reservation.MemberProfile.RemainingClassCredits++;
        }

        reservation.MemberProfile.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return ReservationResult.Ok();
    }
}

public record ReservationResult(bool Succeeded, string? ErrorMessage)
{
    public static ReservationResult Ok()
    {
        return new ReservationResult(true, null);
    }

    public static ReservationResult Fail(string message)
    {
        return new ReservationResult(false, message);
    }
}