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
            (x.LessonAccount == null || x.LessonAccount.PeriodId == x.MemberProfile.LessonPeriodId) &&
            (!x.MemberProfile.MembershipStartsAtUtc.HasValue || x.StartsAtUtc >= x.MemberProfile.MembershipStartsAtUtc.Value) &&
            (!x.MemberProfile.MembershipEndsAtUtc.HasValue || x.StartsAtUtc < x.MemberProfile.MembershipEndsAtUtc.Value))
            .OrderByDescending(x => x.StartsAtUtc).Take(200)
            .Select(x => new { x.StartsAtUtc, x.Status, x.CreditConsumed }).ToListAsync();
        var groups = await db.ClassReservations.AsNoTracking().Where(x => x.MemberProfileId == memberId &&
            (x.LessonAccount == null || x.LessonAccount.PeriodId == x.MemberProfile.LessonPeriodId) &&
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
            .Include(x => x.MembershipPackageOption)
            .Include(x => x.ServicePackageVariant).Where(x => ids.Contains(x.Id)).ToListAsync();
        var personal = await db.PersonalTrainingSessions.AsNoTracking().Where(x => ids.Contains(x.MemberProfileId) &&
            (x.LessonAccount == null || x.LessonAccount.PeriodId == x.MemberProfile.LessonPeriodId) &&
            (!x.MemberProfile.MembershipStartsAtUtc.HasValue || x.StartsAtUtc >= x.MemberProfile.MembershipStartsAtUtc.Value) &&
            (!x.MemberProfile.MembershipEndsAtUtc.HasValue || x.StartsAtUtc < x.MemberProfile.MembershipEndsAtUtc.Value))
            .Select(x => new { x.MemberProfileId, x.Status, x.CreditConsumed }).ToListAsync();
        var groups = await db.ClassReservations.AsNoTracking().Where(x => ids.Contains(x.MemberProfileId) &&
            x.Status == ClassReservationStatus.Reserved &&
            (x.LessonAccount == null || x.LessonAccount.PeriodId == x.MemberProfile.LessonPeriodId) &&
            (!x.MemberProfile.MembershipStartsAtUtc.HasValue || x.ClassSession.StartsAtUtc >= x.MemberProfile.MembershipStartsAtUtc.Value) &&
            (!x.MemberProfile.MembershipEndsAtUtc.HasValue || x.ClassSession.StartsAtUtc < x.MemberProfile.MembershipEndsAtUtc.Value))
            .Select(x => new { x.MemberProfileId, x.ClassSession.Status, Category = x.LessonAccount == null ? x.ClassSession.GroupClass.CreditCategory : x.LessonAccount.Category }).ToListAsync();
        var personalByMember = personal.ToLookup(x => x.MemberProfileId);
        var groupsByMember = groups.ToLookup(x => x.MemberProfileId);
        var result = new Dictionary<int, LessonCreditSummary>();
        foreach (var profile in profiles)
        {
            var lessons = personalByMember[profile.Id].ToList();
            var reservations = groupsByMember[profile.Id].ToList();
            var scheduledGroups = reservations.Count(x => x.Status == ClassSessionStatus.Scheduled);
            var uncharged = lessons.Count(x => !x.CreditConsumed && x.Status != PersonalTrainingSessionStatus.Cancelled);
            var hasSports = profile.MembershipPackage.Code != MembershipPackageCode.KitchenOnly;
            var unlimited = MemberPackageEntitlement.HasUnlimitedClassAccess(profile);
            var chargedPlanned = lessons.Count(x => x.CreditConsumed && x.Status is PersonalTrainingSessionStatus.Scheduled or PersonalTrainingSessionStatus.Postponed);
            int? personalTotal = !hasSports ? 0 : profile.ServicePackageVariant is { } selected
                ? MemberPackageEntitlement.CalculatePersonalTrainingCredits(selected) : profile.MembershipPackageOption?.PersonalTrainingSessionCount;
            int? groupTotal = !hasSports ? 0 : profile.ServicePackageVariant is { } groupVariant
                ? MemberPackageEntitlement.CalculateGroupCredits(groupVariant)
                : profile.MembershipPackageOption?.GroupClassCreditCount;
            var allocation = hasSports ? await new CategoryCreditService(db).PrepareAsync(profile, false) : (Accounts: new List<NO23.Web.Domain.Entities.MemberLessonAccount>(), Error: (string?)null);
            var categoryRows = new List<LessonCategorySummary>();
            foreach (var account in allocation.Accounts)
            {
                var pt = account.Category == LessonCategory.PersonalTraining;
                var planned = pt ? lessons.Count(x => x.Status is PersonalTrainingSessionStatus.Scheduled or PersonalTrainingSessionStatus.Postponed) :
                    reservations.Count(x => x.Category == account.Category && x.Status == ClassSessionStatus.Scheduled);
                var completed = pt ? lessons.Count(x => x.Status is PersonalTrainingSessionStatus.Completed or PersonalTrainingSessionStatus.NoShow) :
                    reservations.Count(x => x.Category == account.Category && x.Status == ClassSessionStatus.Completed);
                categoryRows.Add(new(account.Category, account.Granted, account.ConvertedCredits, account.Remaining + (pt ? 0 : planned), planned,
                    await new CategoryCreditService(db).AvailableAsync(profile, account), completed, account.MonthlyLimit));
            }
            if (categoryRows.Count > 0) {
                personalTotal = categoryRows.Where(x => x.Category == LessonCategory.PersonalTraining).Sum(x => x.Granted);
                groupTotal = categoryRows.Where(x => x.Category != LessonCategory.PersonalTraining).Sum(x => x.Granted);
            }
            result[profile.Id] = new LessonCreditSummary
            {
                ConvertedCredits = categoryRows.Sum(x => x.Converted),
                IsActivationPending = profile.MembershipActivationPending,
                Categories = categoryRows, HasCategoryAccounts = profile.LessonPeriodId.HasValue,
                RequiresCategoryReview = hasSports && allocation.Error is not null && !profile.MembershipActivationPending,
                MemberId = profile.Id, HasSportsPackage = hasSports,
                IsUnlimited = unlimited,
                IsPackageActive = MemberPackageEntitlement.IsActive(profile, DateTime.UtcNow),
                HasKnownPeriod = profile.MembershipStartsAtUtc.HasValue,
                PackageTotal = personalTotal + groupTotal,
                PersonalTrainingTotal = personalTotal, GroupTotal = groupTotal,
                PersonalTrainingCompleted = lessons.Count(x => x.Status is PersonalTrainingSessionStatus.Completed or PersonalTrainingSessionStatus.NoShow),
                PersonalTrainingPlanned = lessons.Count(x => x.Status is PersonalTrainingSessionStatus.Scheduled or PersonalTrainingSessionStatus.Postponed),
                GroupCompleted = reservations.Count(x => x.Status == ClassSessionStatus.Completed),
                GroupPlanned = scheduledGroups,
                StoredBalance = profile.RemainingClassCredits,
                Completed = lessons.Count(x => x.Status is PersonalTrainingSessionStatus.Completed or PersonalTrainingSessionStatus.NoShow)
                    + reservations.Count(x => x.Status == ClassSessionStatus.Completed),
                Planned = lessons.Count(x => x.Status is PersonalTrainingSessionStatus.Scheduled or PersonalTrainingSessionStatus.Postponed) + scheduledGroups,
                // Group bookings already reserve a credit in the stored balance;
                // add only those pending reservations back when displaying remaining lessons.
                Remaining = categoryRows.Count > 0 ? categoryRows.Sum(x => x.Remaining) : hasSports ? profile.RemainingClassCredits + scheduledGroups + chargedPlanned : 0,
                Available = categoryRows.Sum(x => x.Available),
                UnchargedPersonal = uncharged,
                UnreviewedCompleted = hasSports && !unlimited ? lessons.Count(x => !x.CreditConsumed && x.Status is PersonalTrainingSessionStatus.Completed or PersonalTrainingSessionStatus.NoShow) : 0,
                InvalidGroupReservations = hasSports && !unlimited ? reservations.Count(x => x.Status == ClassSessionStatus.Cancelled) : 0
            };
        }
        if (snapshot is not null) await snapshot.CommitAsync();
        return result;
    }
}
