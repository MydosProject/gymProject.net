using System.Data;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class ClassReservationService(ApplicationDbContext dbContext)
{
    public static int CancelSessionReservations(ClassSession session, DateTime nowUtc, string reason)
    {
        var count = 0;
        foreach (var reservation in session.Reservations.Where(item => item.Status == ClassReservationStatus.Reserved))
        {
            reservation.Status = ClassReservationStatus.Cancelled;
            reservation.CancelledAtUtc = nowUtc;
            reservation.CancellationReason = reason;
            if (!MemberPackageEntitlement.HasUnlimitedClassAccess(reservation.MemberProfile))
                reservation.MemberProfile.RemainingClassCredits++;
            reservation.MemberProfile.UpdatedAtUtc = nowUtc;
            count++;
        }
        return count;
    }

    private static readonly TimeSpan CancellationWindow = TimeSpan.FromHours(2);

    public Task<ReservationResult> ReserveAsync(string userId, int classSessionId) => RunAsync(() => ReserveCoreAsync(userId, classSessionId));

    private async Task<ReservationResult> ReserveCoreAsync(string userId, int classSessionId)
    {
        var profile = await dbContext.MemberProfiles
            .Include(member => member.MembershipPackage)
            .FirstOrDefaultAsync(member => member.ApplicationUserId == userId);

        if (profile is null)
        {
            return ReservationResult.Fail("Üye profili bulunamadı.");
        }
        await MemberCreditGuard.LockAsync(dbContext, profile.Id);
        if (dbContext.Database.IsRelational())
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"ClassSessions\" SET \"UpdatedAtUtc\" = \"UpdatedAtUtc\" WHERE \"Id\" = {classSessionId}");

        if (!MemberPackageEntitlement.IsActive(profile, DateTime.UtcNow))
            return ReservationResult.Fail("Üyeliğin sona ermiş. Yeni paket satın alarak ders rezervasyonu yapabilirsin.");

        var session = await dbContext.ClassSessions
            .Include(classSession => classSession.GroupClass)
            .Include(classSession => classSession.Reservations)
            .FirstOrDefaultAsync(classSession => classSession.Id == classSessionId);

        if (session is null)
        {
            return ReservationResult.Fail("Ders programı bulunamadı.");
        }

        if (profile.MembershipStartsAtUtc is { } starts && session.StartsAtUtc < starts)
            return ReservationResult.Fail("Bu ders, paketinin başlangıç tarihinden önce. Paket dönemi içinden bir ders seçebilirsin.");

        if (!MemberPackageEntitlement.IsActive(profile, session.StartsAtUtc))
            return ReservationResult.Fail("Bu ders tarihinde üyeliğin aktif olmayacak.");

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

        var isUnlimitedPackage = MemberPackageEntitlement.HasUnlimitedClassAccess(profile);

        var credits = await new LessonCreditSummaryService(dbContext).GetAsync(profile.Id);
        if (!isUnlimitedPackage && (credits?.Available ?? 0) <= 0)
        {
            return ReservationResult.Fail("Yeni ders için kullanılabilir hakkın bulunmuyor. Planlanmış birebir derslerin de aynı hak bakiyesinden ayrılır.");
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

    public Task<ReservationResult> CancelAsync(string userId, int reservationId) => RunAsync(() => CancelCoreAsync(userId, reservationId));

    private async Task<ReservationResult> CancelCoreAsync(string userId, int reservationId)
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
        await MemberCreditGuard.LockAsync(dbContext, reservation.MemberProfileId);

        if (reservation.Status != ClassReservationStatus.Reserved)
        {
            return ReservationResult.Fail("Bu rezervasyon zaten aktif değil.");
        }

        if (reservation.ClassSession.StartsAtUtc - DateTime.UtcNow < CancellationWindow)
        {
            return ReservationResult.Fail("Rezervasyon iptali ders başlangıcından 2 saat öncesine kadar yapılabilir.");
        }

        reservation.Status = ClassReservationStatus.Cancelled;
        reservation.CancelledAtUtc = DateTime.UtcNow;
        reservation.CancellationReason = "Member cancellation";

        if (!MemberPackageEntitlement.HasUnlimitedClassAccess(reservation.MemberProfile))
        {
            reservation.MemberProfile.RemainingClassCredits++;
        }

        reservation.MemberProfile.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return ReservationResult.Ok();
    }

    public Task<ReservationResult> CancelByAdminAsync(int classSessionId, int reservationId) =>
        RunAsync(() => CancelByAdminCoreAsync(classSessionId, reservationId));

    private async Task<ReservationResult> CancelByAdminCoreAsync(
        int classSessionId,
        int reservationId)
    {
        var reservation = await dbContext.ClassReservations
            .Include(item => item.MemberProfile)
            .ThenInclude(profile => profile.MembershipPackage)
            .FirstOrDefaultAsync(item =>
                item.Id == reservationId &&
                item.ClassSessionId == classSessionId);

        if (reservation is null)
        {
            return ReservationResult.Fail("Rezervasyon bulunamadı.");
        }
        await MemberCreditGuard.LockAsync(dbContext, reservation.MemberProfileId);

        if (reservation.Status != ClassReservationStatus.Reserved)
        {
            return ReservationResult.Fail("Bu rezervasyon zaten aktif değil.");
        }

        var nowUtc = DateTime.UtcNow;
        reservation.Status = ClassReservationStatus.Cancelled;
        reservation.CancelledAtUtc = nowUtc;
        reservation.CancellationReason =
            "Yönetici tarafından ders listesinden çıkarıldı.";

        if (!MemberPackageEntitlement.HasUnlimitedClassAccess(reservation.MemberProfile))
        {
            reservation.MemberProfile.RemainingClassCredits++;
        }

        reservation.MemberProfile.UpdatedAtUtc = nowUtc;

        await dbContext.SaveChangesAsync();
        return ReservationResult.Ok();
    }

    private async Task<ReservationResult> RunAsync(Func<Task<ReservationResult>> operation)
    {
        try
        {
            await using var transaction = dbContext.Database.IsRelational()
                ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
            var result = await operation();
            if (result.Succeeded && transaction is not null) await transaction.CommitAsync();
            return result;
        }
        catch (Exception exception) when (exception is DbUpdateException ||
            exception.GetBaseException() is Npgsql.PostgresException { SqlState: "40001" or "40P01" })
        { return ReservationResult.Fail("Ders veya hak bakiyesi başka bir işlemle değişti. Sayfayı yenileyip tekrar deneyin."); }
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
