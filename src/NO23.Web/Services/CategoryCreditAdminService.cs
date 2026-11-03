using System.Data;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Services;

public sealed class CategoryCreditAdminService(ApplicationDbContext db)
{
    public Task<(bool Succeeded, string Message)> MigrateAsync(CategoryMigrationInput input, string actor) => RunAsync(async () =>
    {
        if (!input.Confirmed || string.IsNullOrWhiteSpace(actor) || string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length < 5 || input.Reason.Length > 400 ||
            input.Balances.Count != 5 || input.Balances.Select(x => x.Category).Distinct().Count() != 5 ||
            input.Balances.Any(x => !Enum.IsDefined(x.Category) || x.Remaining < 0 || x.Granted < x.Remaining || x.Granted > 100000))
            return (false, "Beş kategorinin dağılımını, gerekçeyi ve onayı kontrol edin.");
        await MemberCreditGuard.LockAsync(db, input.MemberId);
        var member = await db.MemberProfiles.Include(x => x.ServicePackageVariant).Include(x => x.MembershipPackage).FirstOrDefaultAsync(x => x.Id == input.MemberId);
        if (member is null || member.MembershipActivationPending || member.MembershipPackage.Code == MembershipPackageCode.KitchenOnly) return (false, "Aktif spor paketinin kaydını kontrol edin.");
        if (member.LessonPeriodId.HasValue) return (false, "Bu üyenin kategori hesapları zaten oluşturulmuş. İkinci kez dağıtılamaz.");
        if (member.RemainingClassCredits != input.ExpectedBalance) return (false, "Bakiye değişti. Sayfayı yenileyin.");
        if (!member.MembershipStartsAtUtc.HasValue && !input.StartsOn.HasValue) return (false, "Eski paketin başlangıç tarihini doğrulayın.");
        var starts = member.MembershipStartsAtUtc ?? ClubTime.ToUtc(input.StartsOn!.Value.ToDateTime(TimeOnly.MinValue));
        var sessions = await db.PersonalTrainingSessions.Where(x => x.MemberProfileId == member.Id && x.StartsAtUtc >= starts &&
            (!member.MembershipEndsAtUtc.HasValue || x.StartsAtUtc < member.MembershipEndsAtUtc) && x.Status != PersonalTrainingSessionStatus.Cancelled).ToListAsync();
        var reservations = await db.ClassReservations.Include(x => x.ClassSession).ThenInclude(x => x.GroupClass).Where(x => x.MemberProfileId == member.Id &&
            x.Status == ClassReservationStatus.Reserved && x.ClassSession.Status != ClassSessionStatus.Cancelled && x.ClassSession.StartsAtUtc >= starts &&
            (!member.MembershipEndsAtUtc.HasValue || x.ClassSession.StartsAtUtc < member.MembershipEndsAtUtc)).ToListAsync();
        var pendingPt = sessions.Count(x => !x.CreditConsumed && x.Status is PersonalTrainingSessionStatus.Scheduled or PersonalTrainingSessionStatus.Postponed);
        if (input.Balances.Single(x => x.Category == LessonCategory.PersonalTraining).Remaining < pendingPt)
            return (false, "PT bakiyesi mevcut planlanmış birebir dersleri karşılamalıdır.");
        member.MembershipStartsAtUtc = starts;
        var period = Guid.NewGuid(); member.LessonPeriodId = period;
        var before = member.RemainingClassCredits;
        member.RemainingClassCredits = input.Balances.Sum(x => x.Remaining);
        foreach (var value in input.Balances)
        {
            var v = member.ServicePackageVariant;
            var account = new MemberLessonAccount { MemberProfile = member, PeriodId = period, Category = value.Category,
                Granted = value.Granted, Remaining = value.Remaining,
                MonthlyLimit = v?.LessonsRenewMonthly == true ? CategoryCreditService.Grant(v, value.Category) / Math.Max(1, v.DurationMonths ?? 1) : null };
            account.Movements.Add(new LessonCreditMovement { Delta = value.Remaining, Actor = actor,
                Reason = $"Yönetici kategori geçişi; eski toplam {before}, yeni toplam {member.RemainingClassCredits}. {input.Reason.Trim()}" });
            member.LessonAccounts.Add(account);
        }
        foreach (var session in sessions) {
            session.LessonAccount = member.LessonAccounts.Single(x => x.PeriodId == period && x.Category == LessonCategory.PersonalTraining);
            if (session.Status is PersonalTrainingSessionStatus.Completed or PersonalTrainingSessionStatus.NoShow) session.CreditConsumed = true;
        }
        foreach (var reservation in reservations) reservation.LessonAccount = member.LessonAccounts.Single(x => x.PeriodId == period && x.Category == reservation.ClassSession.GroupClass.CreditCategory);
        await db.SaveChangesAsync();
        return (true, "Kategori dağılımı gerekçesiyle kaydedildi. Bundan sonraki işlemler ayrı hesaplardan yapılır.");
    });

    public Task<(bool Succeeded, string Message)> ConvertAsync(CreditConversionInput input, string actor) => RunAsync(async () =>
    {
        if (input.OperationId == Guid.Empty || input.Times is < 1 or > 1000 || string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length < 5 || input.Reason.Length > 300 || string.IsNullOrWhiteSpace(actor)) return (false, "Dönüşüm bilgilerini kontrol edin.");
        await MemberCreditGuard.LockAsync(db, input.MemberId);
        var member = await db.MemberProfiles.Include(x => x.MembershipPackage).FirstOrDefaultAsync(x => x.Id == input.MemberId);
        if (member?.LessonPeriodId is null || !MemberPackageEntitlement.IsActive(member, DateTime.UtcNow)) return (false, "Önce kategori dağılımını ve paket dönemini doğrulayın.");
        if (await db.LessonCreditMovements.AnyAsync(x => x.Account.MemberProfileId == member.Id && x.OperationId == input.OperationId)) return (true, "Bu dönüşüm daha önce işlendi; ikinci kez uygulanmadı.");
        var rule = await db.LessonConversionRules.FirstOrDefaultAsync(x => x.Id == input.RuleId && x.IsActive);
        if (rule is null || rule.From == rule.To || rule.SourceCredits <= 0 || rule.TargetCredits <= 0) return (false, "Aktif ve geçerli bir dönüşüm kuralı seçin.");
        var accounts = await db.MemberLessonAccounts.Where(x => x.MemberProfileId == member.Id && x.PeriodId == member.LessonPeriodId).ToListAsync();
        if (accounts.Count != 5 || accounts.Select(x => x.Category).Distinct().Count() != 5)
            return (false, CategoryCreditService.ReviewMessage);
        var from = accounts.Single(x => x.Category == rule.From); var to = accounts.Single(x => x.Category == rule.To);
        var source = checked(rule.SourceCredits * input.Times); var target = checked(rule.TargetCredits * input.Times);
        if (target > 100000 || source > await new CategoryCreditService(db).AvailableAsync(member, from)) return (false, "Kaynak kategoride dönüşüm için yeterli boş hak yok. Planlı PT hakları dönüştürülemez.");
        CategoryCreditService.Change(member, from, -source, actor, $"Kural #{rule.Id} {rule.Name}: {input.Reason.Trim()}", input.OperationId);
        CategoryCreditService.Change(member, to, target, actor, $"Kural #{rule.Id} {rule.Name}: {input.Reason.Trim()}", input.OperationId);
        from.ConvertedCredits -= source; to.ConvertedCredits += target;
        if (rule.IncreaseTargetMonthlyLimit && to.MonthlyLimit.HasValue) to.MonthlyLimit += target;
        await db.SaveChangesAsync();
        return (true, "Dönüşüm ve aylık sınır tercihi seçilen kurala göre uygulandı.");
    });

    // Manual corrections (compensation, gifts, wrong records) are logged as movements and counted with
    // conversions, so the package total keeps matching remaining plus completed lessons.
    public Task<(bool Succeeded, string Message)> AdjustAsync(CreditAdjustmentInput input, string actor) => RunAsync(async () =>
    {
        if (input.OperationId == Guid.Empty || !Enum.IsDefined(input.Category) || input.Delta is < -1000 or > 1000 ||
            input.MonthlyLimit is < 0 or > 1000 || string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length < 5 ||
            input.Reason.Length > 300 || string.IsNullOrWhiteSpace(actor))
            return (false, "Düzeltme bilgilerini kontrol edin.");
        if (input.Delta == 0 && !input.ChangeMonthlyLimit) return (false, "Eklenecek veya çıkarılacak hak ya da yeni aylık sınır girin.");
        await MemberCreditGuard.LockAsync(db, input.MemberId);
        var member = await db.MemberProfiles.Include(x => x.MembershipPackage).FirstOrDefaultAsync(x => x.Id == input.MemberId);
        if (member?.LessonPeriodId is null) return (false, "Önce kategori dağılımını doğrulayın.");
        if (await db.LessonCreditMovements.AnyAsync(x => x.Account.MemberProfileId == member.Id && x.OperationId == input.OperationId))
            return (true, "Bu düzeltme daha önce işlendi; ikinci kez uygulanmadı.");
        var account = await db.MemberLessonAccounts.FirstOrDefaultAsync(x =>
            x.MemberProfileId == member.Id && x.PeriodId == member.LessonPeriodId && x.Category == input.Category);
        if (account is null) return (false, CategoryCreditService.ReviewMessage);
        var limitChanges = input.ChangeMonthlyLimit && account.MonthlyLimit != input.MonthlyLimit;
        if (input.Delta == 0 && !limitChanges) return (false, "Aylık sınır zaten bu değerde.");
        if (input.Delta != 0 && account.IsUnlimited) return (false, "Sınırsız kategoride hak düzeltmesi yapılmaz.");
        if (input.Delta < 0 && -input.Delta > await new CategoryCreditService(db).AvailableAsync(member, account))
            return (false, "Çıkarılacak hak, kategorideki boş haktan fazla. Planlı derslere ayrılmış haklar çıkarılamaz.");
        // One movement per account and operation (unique index), so a limit change rides on the credit movement.
        var limitNote = limitChanges ? $"Aylık sınır {account.MonthlyLimit?.ToString() ?? "yok"} → {input.MonthlyLimit?.ToString() ?? "yok"}" : null;
        var reason = input.Reason.Trim();
        if (input.Delta != 0)
        {
            CategoryCreditService.Change(member, account, input.Delta, actor,
                limitNote is null ? $"Yönetici düzeltmesi: {reason}" : $"Yönetici düzeltmesi, {limitNote}: {reason}", input.OperationId);
            account.ConvertedCredits += input.Delta;
        }
        else if (limitNote is not null)
            account.Movements.Add(new LessonCreditMovement { Delta = 0, Actor = actor, OperationId = input.OperationId, Reason = $"{limitNote}: {reason}" });
        if (limitChanges) account.MonthlyLimit = input.MonthlyLimit;
        member.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return (true, "Hak düzeltmesi kaydedildi.");
    });

    private async Task<(bool Succeeded, string Message)> RunAsync(Func<Task<(bool Succeeded, string Message)>> operation)
    {
        try {
            await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
            var result = await operation(); if (result.Succeeded && tx is not null) await tx.CommitAsync(); return result;
        } catch (Exception e) when (e is DbUpdateException || e.GetBaseException() is Npgsql.PostgresException { SqlState: "40001" or "40P01" }) {
            return (false, "Haklar başka bir işlemle değişti. Sayfayı yenileyin.");
        }
    }
}
