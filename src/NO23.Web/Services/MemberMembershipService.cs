using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Extensions;

namespace NO23.Web.Services;

public class MemberMembershipService(ApplicationDbContext dbContext)
{
    private const string MemberCancellationReason =
        "Member requested period-end cancellation";

    public static bool IsActiveForUse(MemberProfile profile, DateTime nowUtc)
    {
        return IsActiveForUse(
            profile.MembershipStatus,
            profile.MembershipEndsAtUtc,
            nowUtc);
    }

    public static bool IsActiveForUse(
        MembershipStatus status,
        DateTime membershipEndsAtUtc,
        DateTime nowUtc)
    {
        return membershipEndsAtUtc > nowUtc &&
            status is (MembershipStatus.Active or
                MembershipStatus.CancellationScheduled);
    }

    public static string GetEffectiveStatusDisplayName(
        MemberProfile profile,
        DateTime nowUtc)
    {
        return GetEffectiveStatusDisplayName(
            profile.MembershipStatus,
            profile.MembershipEndsAtUtc,
            nowUtc);
    }

    public static string GetEffectiveStatusDisplayName(
        MembershipStatus status,
        DateTime membershipEndsAtUtc,
        DateTime nowUtc)
    {
        return GetEffectiveStatus(status, membershipEndsAtUtc, nowUtc)
            .GetDisplayName();
    }

    public static MembershipStatus GetEffectiveStatus(
        MembershipStatus status,
        DateTime membershipEndsAtUtc,
        DateTime nowUtc)
    {
        if (membershipEndsAtUtc <= nowUtc &&
            status == MembershipStatus.CancellationScheduled)
        {
            return MembershipStatus.Cancelled;
        }

        if (membershipEndsAtUtc <= nowUtc &&
            status == MembershipStatus.Active)
        {
            return MembershipStatus.Expired;
        }

        return status;
    }

    public static bool CanReserveClassSession(
        MemberProfile profile,
        DateTime sessionStartsAtUtc,
        DateTime nowUtc,
        out string errorMessage)
    {
        if (!IsActiveForUse(profile, nowUtc))
        {
            errorMessage =
                "Üyelik paketin aktif olmadığı için rezervasyon yapılamaz.";
            return false;
        }

        if (sessionStartsAtUtc >= profile.MembershipEndsAtUtc)
        {
            errorMessage =
                "Bu ders paket bitiş tarihinden sonra olduğu için rezervasyon yapılamaz.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public static bool CanRequestPersonalTraining(
        MemberProfile profile,
        DateOnly preferredDate,
        DateTime nowUtc,
        out string errorMessage)
    {
        if (!IsActiveForUse(profile, nowUtc))
        {
            errorMessage =
                "Üyelik paketin aktif olmadığı için birebir talep oluşturulamaz.";
            return false;
        }

        var membershipEndsOn =
            DateOnly.FromDateTime(profile.MembershipEndsAtUtc.ToLocalTime());

        if (preferredDate > membershipEndsOn)
        {
            errorMessage =
                "Bu tarih paket bitişinden sonra olduğu için birebir talep oluşturulamaz.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public async Task<MemberMembershipResult> ScheduleCancellationAsync(
        string userId)
    {
        var nowUtc = DateTime.UtcNow;

        var profile = await dbContext.MemberProfiles
            .Include(member => member.MembershipPackage)
            .Include(member => member.ClassReservations)
            .ThenInclude(reservation => reservation.ClassSession)
            .FirstOrDefaultAsync(member => member.ApplicationUserId == userId);

        if (profile is null)
        {
            return MemberMembershipResult.Fail("Üye profili bulunamadı.");
        }

        if (profile.MembershipStatus == MembershipStatus.CancellationScheduled)
        {
            return MemberMembershipResult.Fail(
                "Üyelik paketin zaten dönem sonunda iptal edilecek.");
        }

        if (!IsActiveForUse(profile, nowUtc))
        {
            if (profile.MembershipEndsAtUtc <= nowUtc &&
                profile.MembershipStatus is MembershipStatus.Active)
            {
                profile.MembershipStatus = MembershipStatus.Expired;
                profile.UpdatedAtUtc = nowUtc;
                await dbContext.SaveChangesAsync();
            }

            return MemberMembershipResult.Fail(
                "Aktif üyelik paketi bulunamadı.");
        }

        profile.MembershipStatus = MembershipStatus.CancellationScheduled;
        profile.MembershipCancellationRequestedAtUtc = nowUtc;
        profile.MembershipCancellationEffectiveAtUtc =
            profile.MembershipEndsAtUtc;
        profile.MembershipCancellationReason = MemberCancellationReason;
        profile.UpdatedAtUtc = nowUtc;

        var cancelledReservationCount = 0;

        foreach (var reservation in profile.ClassReservations.Where(reservation =>
            reservation.Status == ClassReservationStatus.Reserved &&
            reservation.ClassSession.StartsAtUtc >= profile.MembershipEndsAtUtc))
        {
            reservation.Status = ClassReservationStatus.Cancelled;
            reservation.CancelledAtUtc = nowUtc;
            reservation.CancellationReason =
                "Membership cancellation scheduled";

            if (profile.MembershipPackage.WeeklyClassLimit is not null)
            {
                profile.RemainingClassCredits++;
            }

            cancelledReservationCount++;
        }

        await dbContext.SaveChangesAsync();

        return MemberMembershipResult.Ok(cancelledReservationCount);
    }

    public async Task<MemberMembershipResult> RequestPackageChangeAsync(
        string userId,
        int requestedPackageId)
    {
        var profile = await dbContext.MemberProfiles
            .FirstOrDefaultAsync(member => member.ApplicationUserId == userId);

        if (profile is null)
        {
            return MemberMembershipResult.Fail("Üye profili bulunamadı.");
        }

        var requestedPackage = await dbContext.MembershipPackages
            .AsNoTracking()
            .FirstOrDefaultAsync(package =>
                package.Id == requestedPackageId &&
                package.IsActive);

        if (requestedPackage is null)
        {
            return MemberMembershipResult.Fail(
                "Geçerli bir üyelik paketi seçmelisin.");
        }

        var hasPendingRequest =
            await dbContext.MembershipPackageChangeRequests
                .AnyAsync(request =>
                    request.MemberProfileId == profile.Id &&
                    request.Status ==
                        MembershipPackageChangeRequestStatus.Pending);

        if (hasPendingRequest)
        {
            return MemberMembershipResult.Fail(
                "Devam eden paket talebin var.");
        }

        dbContext.MembershipPackageChangeRequests.Add(
            new MembershipPackageChangeRequest
            {
                MemberProfileId = profile.Id,
                CurrentMembershipPackageId = profile.MembershipPackageId,
                RequestedMembershipPackageId = requestedPackage.Id,
                RequestedAtUtc = DateTime.UtcNow
            });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return MemberMembershipResult.Fail(
                "Devam eden paket talebin var.");
        }

        return MemberMembershipResult.Ok(0);
    }

    public async Task<MemberMembershipResult> ApprovePackageChangeRequestAsync(
        int requestId,
        string adminUserId,
        string? adminNote)
    {
        var request = await dbContext.MembershipPackageChangeRequests
            .Include(item => item.MemberProfile)
            .Include(item => item.RequestedMembershipPackage)
            .FirstOrDefaultAsync(item => item.Id == requestId);

        if (request is null)
        {
            return MemberMembershipResult.Fail("Paket talebi bulunamadı.");
        }

        if (request.Status != MembershipPackageChangeRequestStatus.Pending)
        {
            return MemberMembershipResult.Fail(
                "Yalnızca bekleyen paket talepleri onaylanabilir.");
        }

        if (!request.RequestedMembershipPackage.IsActive)
        {
            return MemberMembershipResult.Fail(
                "Seçilen üyelik paketi artık aktif değil.");
        }

        var nowUtc = DateTime.UtcNow;
        var periodStartsAtUtc =
            request.MemberProfile.MembershipEndsAtUtc > nowUtc
                ? request.MemberProfile.MembershipEndsAtUtc
                : nowUtc;

        request.Status = MembershipPackageChangeRequestStatus.Approved;
        request.ResolvedAtUtc = nowUtc;
        request.ResolvedByUserId = adminUserId;
        request.AdminNote = adminNote?.Trim();

        request.MemberProfile.MembershipPackageId =
            request.RequestedMembershipPackageId;
        request.MemberProfile.MembershipStartsAtUtc = periodStartsAtUtc;
        request.MemberProfile.MembershipEndsAtUtc =
            periodStartsAtUtc.AddDays(
                MemberProfile.DefaultMembershipDurationDays);
        request.MemberProfile.MembershipStatus = MembershipStatus.Active;
        request.MemberProfile.MembershipCancellationRequestedAtUtc = null;
        request.MemberProfile.MembershipCancellationEffectiveAtUtc = null;
        request.MemberProfile.MembershipCancellationReason = null;
        request.MemberProfile.RemainingClassCredits =
            CalculateInitialClassCredits(request.RequestedMembershipPackage);
        request.MemberProfile.UpdatedAtUtc = nowUtc;

        await dbContext.SaveChangesAsync();

        return MemberMembershipResult.Ok(0);
    }

    public async Task<MemberMembershipResult> RejectPackageChangeRequestAsync(
        int requestId,
        string adminUserId,
        string? adminNote)
    {
        var request = await dbContext.MembershipPackageChangeRequests
            .FirstOrDefaultAsync(item => item.Id == requestId);

        if (request is null)
        {
            return MemberMembershipResult.Fail("Paket talebi bulunamadı.");
        }

        if (request.Status != MembershipPackageChangeRequestStatus.Pending)
        {
            return MemberMembershipResult.Fail(
                "Yalnızca bekleyen paket talepleri reddedilebilir.");
        }

        request.Status = MembershipPackageChangeRequestStatus.Rejected;
        request.ResolvedAtUtc = DateTime.UtcNow;
        request.ResolvedByUserId = adminUserId;
        request.AdminNote = adminNote?.Trim();

        await dbContext.SaveChangesAsync();

        return MemberMembershipResult.Ok(0);
    }

    private static int CalculateInitialClassCredits(
        MembershipPackage package)
    {
        return package.WeeklyClassLimit.HasValue
            ? package.WeeklyClassLimit.Value * 4
            : 0;
    }
}

public sealed record MemberMembershipResult(
    bool Succeeded,
    string? ErrorMessage,
    int CancelledReservationCount = 0)
{
    public static MemberMembershipResult Ok(int cancelledReservationCount)
    {
        return new MemberMembershipResult(
            true,
            null,
            cancelledReservationCount);
    }

    public static MemberMembershipResult Fail(string message)
    {
        return new MemberMembershipResult(false, message);
    }
}
