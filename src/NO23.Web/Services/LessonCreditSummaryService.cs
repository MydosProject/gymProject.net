using System.Data;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Member;
using NO23.Web.ViewModels.Admin;
using NO23.Web.Extensions;

namespace NO23.Web.Services;

public sealed class LessonCreditSummaryService(ApplicationDbContext db)
{
    public async Task<IReadOnlyList<MemberCreditHistoryRow>> HistoryAsync(int memberId)
    {
        var personal = await db.PersonalTrainingSessions.AsNoTracking().Where(x => x.MemberProfileId == memberId &&
            (!x.MemberProfile.MembershipStartsAtUtc.HasValue || x.StartsAtUtc >= x.MemberProfile.MembershipStartsAtUtc.Value) &&
            (!x.MemberProfile.MembershipEndsAtUtc.HasValue || x.StartsAtUtc < x.MemberProfile.MembershipEndsAtUtc.Value))
            .OrderByDescending(x => x.StartsAtUtc).Take(200)
            .Select(x => new { x.StartsAtUtc, x.Status, x.CreditConsumed }).ToListAsync();
        var groups = await db.ClassReservations.AsNoTracking().Where(x => x.MemberProfileId == memberId &&
            (!x.MemberProfile.MembershipStartsAtUtc.HasValue || x.ClassSession.StartsAtUtc >= x.MemberProfile.MembershipStartsAtUtc.Value) &&
            (!x.MemberProfile.MembershipEndsAtUtc.HasValue || x.ClassSession.StartsAtUtc < x.MemberProfile.MembershipEndsAtUtc.Value))
            .OrderByDescending(x => x.ClassSession.StartsAtUtc).Take(200)
            .Select(x => new { x.ClassSession.StartsAtUtc, ReservationStatus = x.Status, x.ClassSession.Status, x.ClassSession.GroupClass.Name }).ToListAsync();
        return personal.Select(x => new MemberCreditHistoryRow(x.StartsAtUtc, "Birebir", "Birebir seans", x.Status.GetDisplayName(),
                x.CreditConsumed ? "Hak işlenmiş" : x.Status == PersonalTrainingSessionStatus.Cancelled ? "İptal" : "Hak ayrılmış / kontrol bekliyor"))
            .Concat(groups.Select(x => new MemberCreditHistoryRow(x.StartsAtUtc, "Grup", x.Name, x.Status.GetDisplayName(),
                x.ReservationStatus == ClassReservationStatus.Cancelled ? "Rezervasyon iptal" :
                x.Status == ClassSessionStatus.Cancelled ? "İade kontrol edilmeli" : x.Status == ClassSessionStatus.Completed ? "Hak kullanılmış" : "Hak ayrılmış")))
            .OrderByDescending(x => x.StartsAtUtc).Take(200).ToList();
    }

    public async Task<LessonCreditSummary?> GetAsync(int memberId) =>
        (await LoadAsync([memberId])).GetValueOrDefault(memberId);

    public async Task<IReadOnlyDictionary<int, LessonCreditSummary>> LoadAsync(IEnumerable<int> memberIds)
    {
        var ids = memberIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<int, LessonCreditSummary>();
        // Keep the balance and the lesson counts in the same database snapshot.
        await using var snapshot = db.Database.IsRelational() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead) : null;
        var profiles = await db.MemberProfiles.AsNoTracking().Include(x => x.MembershipPackage)
            .Include(x => x.ServicePackageVariant).Where(x => ids.Contains(x.Id)).ToListAsync();
        var personal = await db.PersonalTrainingSessions.AsNoTracking().Where(x => ids.Contains(x.MemberProfileId) &&
            (!x.MemberProfile.MembershipStartsAtUtc.HasValue || x.StartsAtUtc >= x.MemberProfile.MembershipStartsAtUtc.Value) &&
            (!x.MemberProfile.MembershipEndsAtUtc.HasValue || x.StartsAtUtc < x.MemberProfile.MembershipEndsAtUtc.Value))
            .Select(x => new { x.MemberProfileId, x.Status, x.CreditConsumed }).ToListAsync();
        var groups = await db.ClassReservations.AsNoTracking().Where(x => ids.Contains(x.MemberProfileId) &&
            x.Status == ClassReservationStatus.Reserved &&
            (!x.MemberProfile.MembershipStartsAtUtc.HasValue || x.ClassSession.StartsAtUtc >= x.MemberProfile.MembershipStartsAtUtc.Value) &&
            (!x.MemberProfile.MembershipEndsAtUtc.HasValue || x.ClassSession.StartsAtUtc < x.MemberProfile.MembershipEndsAtUtc.Value))
            .Select(x => new { x.MemberProfileId, x.ClassSession.Status }).ToListAsync();
        var personalByMember = personal.ToLookup(x => x.MemberProfileId);
        var groupsByMember = groups.ToLookup(x => x.MemberProfileId);
        var result = profiles.ToDictionary(profile => profile.Id, profile =>
        {
            var lessons = personalByMember[profile.Id].ToList();
            var reservations = groupsByMember[profile.Id].ToList();
            var scheduledGroups = reservations.Count(x => x.Status == ClassSessionStatus.Scheduled);
            var uncharged = lessons.Count(x => !x.CreditConsumed && x.Status != PersonalTrainingSessionStatus.Cancelled);
            var hasSports = profile.MembershipPackage.Code != MembershipPackageCode.KitchenOnly;
            var unlimited = MemberPackageEntitlement.HasUnlimitedClassAccess(profile);
            var chargedPlanned = lessons.Count(x => x.CreditConsumed && x.Status is PersonalTrainingSessionStatus.Scheduled or PersonalTrainingSessionStatus.Postponed);
            return new LessonCreditSummary
            {
                MemberId = profile.Id, HasSportsPackage = hasSports,
                IsUnlimited = unlimited,
                IsPackageActive = MemberPackageEntitlement.IsActive(profile, DateTime.UtcNow),
                HasKnownPeriod = profile.MembershipStartsAtUtc.HasValue,
                PackageTotal = !hasSports ? 0 : profile.ServicePackageVariant is { } variant
                    ? MemberPackageEntitlement.CalculateInitialCredits(variant) : null,
                StoredBalance = profile.RemainingClassCredits,
                Completed = lessons.Count(x => x.Status is PersonalTrainingSessionStatus.Completed or PersonalTrainingSessionStatus.NoShow)
                    + reservations.Count(x => x.Status == ClassSessionStatus.Completed),
                Planned = lessons.Count(x => x.Status is PersonalTrainingSessionStatus.Scheduled or PersonalTrainingSessionStatus.Postponed) + scheduledGroups,
                // Group bookings already reserve a credit in the stored balance;
                // add only those pending reservations back when displaying remaining lessons.
                Remaining = hasSports ? profile.RemainingClassCredits + scheduledGroups + chargedPlanned : 0,
                Available = hasSports ? Math.Max(0, profile.RemainingClassCredits - uncharged) : 0,
                UnchargedPersonal = uncharged,
                UnreviewedCompleted = hasSports && !unlimited ? lessons.Count(x => !x.CreditConsumed && x.Status is PersonalTrainingSessionStatus.Completed or PersonalTrainingSessionStatus.NoShow) : 0,
                InvalidGroupReservations = hasSports && !unlimited ? reservations.Count(x => x.Status == ClassSessionStatus.Cancelled) : 0
            };
        });
        if (snapshot is not null) await snapshot.CommitAsync();
        return result;
    }
}
