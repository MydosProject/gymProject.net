using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public sealed class CategoryCreditService(ApplicationDbContext db)
{
    public const string ReviewMessage = "Eski ortak hak bakiyesi kategorilere güvenle ayrılamıyor. Yönetici, Üyeler > Ders hakları ekranından dağılımı doğrulamalı.";
    public static int Grant(ServicePackageVariant v, LessonCategory category) => (category switch
    {
        LessonCategory.PersonalTraining => v.PersonalTrainingSessionCount,
        LessonCategory.Performance => v.PerformanceClassCreditCount,
        LessonCategory.Reformer => v.ReformerClassCreditCount,
        LessonCategory.Group => v.GroupClassCreditCount,
        LessonCategory.Kids => v.KidsClassCreditCount,
        _ => 0
    }) * (v.LessonsRenewMonthly ? Math.Max(1, v.DurationMonths ?? 1) : 1);

    public static void Initialize(MemberProfile member, ServicePackageVariant variant, string actor)
    {
        var period = Guid.NewGuid();
        member.LessonPeriodId = period;
        member.MembershipActivationPending = false;
        member.RemainingClassCredits = 0;
        foreach (var category in Enum.GetValues<LessonCategory>())
        {
            var grant = Grant(variant, category);
            var months = variant.LessonsRenewMonthly ? Math.Max(1, variant.DurationMonths ?? 1) : 1;
            var account = new MemberLessonAccount { MemberProfile = member, PeriodId = period, Category = category,
                Granted = grant, Remaining = grant, MonthlyLimit = variant.LessonsRenewMonthly ? grant / months : null };
            account.Movements.Add(new LessonCreditMovement { Delta = grant, Actor = actor, Reason = "Paket aktivasyonu: kategori hakkı tanımlandı." });
            member.LessonAccounts.Add(account);
            member.RemainingClassCredits += grant;
        }
    }

    // A legacy allocation is adopted only when its recorded total and every typed
    // booking reconcile. Ambiguous history is never assigned to an arbitrary category.
    public async Task<(List<MemberLessonAccount> Accounts, string? Error)> PrepareAsync(MemberProfile member, bool apply)
    {
        if (member.MembershipActivationPending) return ([], "Paket ödemesi veya yönetici onayı bekleniyor.");
        if (member.LessonPeriodId is { } period)
        {
            var query = db.MemberLessonAccounts.Where(x => x.MemberProfileId == member.Id && x.PeriodId == period);
            var accounts = apply ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();
            if (accounts.Count == 0) accounts = member.LessonAccounts.Where(x => x.PeriodId == period).ToList();
            return accounts.Count == 5 ? (accounts, null) : ([], ReviewMessage);
        }
        var variant = member.ServicePackageVariant ?? (member.ServicePackageVariantId.HasValue
            ? await db.ServicePackageVariants.FirstOrDefaultAsync(x => x.Id == member.ServicePackageVariantId) : null);
        var option = member.MembershipPackageOption ?? (member.MembershipPackageOptionId.HasValue
            ? await db.MembershipPackageOptions.FirstOrDefaultAsync(x => x.Id == member.MembershipPackageOptionId) : null);
        if (variant is null && option is not null) variant = new ServicePackageVariant {
            PersonalTrainingSessionCount = option.PersonalTrainingSessionCount, GroupClassCreditCount = option.GroupClassCreditCount };
        if (variant is null || !member.MembershipStartsAtUtc.HasValue) return ([], ReviewMessage);
        var personal = await db.PersonalTrainingSessions.Where(x => x.MemberProfileId == member.Id && x.Status != PersonalTrainingSessionStatus.Cancelled &&
            x.StartsAtUtc >= member.MembershipStartsAtUtc && (!member.MembershipEndsAtUtc.HasValue || x.StartsAtUtc < member.MembershipEndsAtUtc)).ToListAsync();
        var reservations = await db.ClassReservations.Include(x => x.ClassSession).ThenInclude(x => x.GroupClass)
            .Where(x => x.MemberProfileId == member.Id && x.Status == ClassReservationStatus.Reserved && x.ClassSession.Status != ClassSessionStatus.Cancelled &&
                x.ClassSession.StartsAtUtc >= member.MembershipStartsAtUtc && (!member.MembershipEndsAtUtc.HasValue || x.ClassSession.StartsAtUtc < member.MembershipEndsAtUtc)).ToListAsync();
        if (personal.Any(x => !x.CreditConsumed && x.Status is PersonalTrainingSessionStatus.Completed or PersonalTrainingSessionStatus.NoShow)) return ([], ReviewMessage);
        var key = Guid.NewGuid();
        var projected = Enum.GetValues<LessonCategory>().Select(category => new MemberLessonAccount {
            MemberProfileId = member.Id, PeriodId = key, Category = category, Granted = Grant(variant, category),
            Remaining = Grant(variant, category) - (category == LessonCategory.PersonalTraining ? personal.Count(x => x.CreditConsumed) :
                reservations.Count(x => x.ClassSession.GroupClass.CreditCategory == category)),
            MonthlyLimit = variant.LessonsRenewMonthly ? Grant(variant, category) / Math.Max(1, variant.DurationMonths ?? 1) : null
        }).ToList();
        if (projected.Any(x => x.Remaining < 0) || projected.Sum(x => x.Remaining) != member.RemainingClassCredits) return ([], ReviewMessage);
        if (apply)
        {
            member.LessonPeriodId = key;
            foreach (var account in projected) {
                member.LessonAccounts.Add(account);
                account.Movements.Add(new LessonCreditMovement { Delta = account.Remaining, Actor = "system:legacy-reconciliation", Reason = "Eski dönem: kategori geçmişi ve kayıtlı bakiye eşleştirildi." });
            }
            foreach (var session in personal) session.LessonAccount = projected.Single(x => x.Category == LessonCategory.PersonalTraining);
            foreach (var reservation in reservations) reservation.LessonAccount = projected.Single(x => x.Category == reservation.ClassSession.GroupClass.CreditCategory);
        }
        return (projected, null);
    }

    public async Task<int> AvailableAsync(MemberProfile member, MemberLessonAccount account, int? excludedSession = null)
    {
        if (account.IsUnlimited) return int.MaxValue;
        if (account.Category != LessonCategory.PersonalTraining) return Math.Max(0, account.Remaining);
        var pending = await db.PersonalTrainingSessions.CountAsync(x => x.MemberProfileId == member.Id && x.Id != excludedSession &&
            !x.CreditConsumed && x.Status != PersonalTrainingSessionStatus.Cancelled &&
            ((account.Id > 0 && x.LessonAccountId == account.Id) ||
             (x.LessonAccountId == null && (!member.MembershipStartsAtUtc.HasValue || x.StartsAtUtc >= member.MembershipStartsAtUtc) &&
                (!member.MembershipEndsAtUtc.HasValue || x.StartsAtUtc < member.MembershipEndsAtUtc))));
        return Math.Max(0, account.Remaining - pending);
    }

    public static void Change(MemberProfile member, MemberLessonAccount account, int delta, string actor, string reason, Guid? operationId = null)
    {
        if (account.IsUnlimited) return;
        if (account.Remaining + delta < 0) throw new InvalidOperationException("Kategori bakiyesi yetersiz.");
        account.Remaining += delta;
        account.Movements.Add(new LessonCreditMovement { Delta = delta, Actor = actor, Reason = reason, OperationId = operationId });
        if (member.LessonPeriodId == account.PeriodId) member.RemainingClassCredits += delta;
    }

    public static void Refund(ClassReservation reservation, string actor, string reason)
    {
        if (reservation.LessonAccount is { } account) Change(reservation.MemberProfile, account, 1, actor, reason);
        else if (reservation.MemberProfile.LessonPeriodId is null && !MemberPackageEntitlement.HasUnlimitedClassAccess(reservation.MemberProfile))
            reservation.MemberProfile.RemainingClassCredits++;
    }
}
