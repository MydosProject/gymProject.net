using System.Data;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Web.Services;

public partial class PersonalTrainingCalendarService(ApplicationDbContext dbContext)
{
    public Task<(bool Succeeded, string Message)> CreateWeeklyAsync(
        int trainerId, WeeklyPersonalSessionInput input) =>
        CreateWeeklyCoreAsync(
            trainerId,
            input,
            allowUnassignedMember: false,
            allowPastDates: false);

    public Task<(bool Succeeded, string Message)> CreateWeeklyByAdminAsync(
        int trainerId, WeeklyPersonalSessionInput input) =>
        CreateWeeklyCoreAsync(
            trainerId,
            input,
            allowUnassignedMember: true,
            allowPastDates: true);

    private async Task<(bool Succeeded, string Message)> CreateWeeklyCoreAsync(
        int trainerId,
        WeeklyPersonalSessionInput input,
        bool allowUnassignedMember,
        bool allowPastDates)
    {
        try
        {
            await using var transaction = dbContext.Database.IsRelational()
                ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
            await TrainerScheduleGuard.LockAsync(dbContext, trainerId);
            await MemberCreditGuard.LockAsync(dbContext, input.MemberProfileId);
            var plan = await BuildWeeklyPlanAsync(trainerId, input, allowUnassignedMember, allowPastDates);
            if (!plan.Succeeded) return (false, plan.Message);
            var recurrenceIds = input.Days.Distinct().ToDictionary(day => day, _ => Guid.NewGuid());
            dbContext.PersonalTrainingSessions.AddRange(plan.Starts.Select(start => new PersonalTrainingSession
            {
                LessonAccount = plan.PersonalAccount,
                TrainerId = trainerId, MemberProfileId = input.MemberProfileId, StartsAtUtc = start,
                RecurrenceId = recurrenceIds[ClubTime.ToLocal(start).DayOfWeek],
                DurationMinutes = input.DurationMinutes, Note = input.Note?.Trim()
            }));
            await dbContext.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
            return (true, $"{plan.LessonCount} birebir ders haftalık programa eklendi. {plan.Message}");
        }
        catch (Exception exception) when (exception is DbUpdateException ||
            exception.GetBaseException() is Npgsql.PostgresException { SqlState: "40001" or "40P01" })
        { return (false, "Program başka bir işlemle değişti. Takvimi yenileyip tekrar deneyin."); }
    }

    private async Task<string?> ValidateEntitlementsAsync(
        MemberProfile member,
        IReadOnlyList<DateTime> starts,
        DateTime nowUtc,
        int durationMinutes,
        bool allowPastDates = false,
        int? excludedSessionId = null)
    {
        if ((!allowPastDates && !MemberPackageEntitlement.IsActive(member, nowUtc)) ||
            starts.Any(start => !MemberPackageEntitlement.IsActive(member, start) ||
                (member.MembershipStartsAtUtc.HasValue && start < member.MembershipStartsAtUtc.Value) ||
                (member.MembershipEndsAtUtc.HasValue && start.AddMinutes(durationMinutes) > member.MembershipEndsAtUtc.Value)))
            return "Ders tarihleri üyenin aktif üyelik dönemi içinde olmalı.";

        var creditService = new CategoryCreditService(dbContext);
        var allocation = await creditService.PrepareAsync(member, true);
        if (allocation.Error is not null) return allocation.Error;
        var account = allocation.Accounts.Single(x => x.Category == LessonCategory.PersonalTraining);
        if (starts.Count > await creditService.AvailableAsync(member, account, excludedSessionId))
            return "Planlanan ders sayısı kullanılabilir PT hakkını aşıyor. Grup veya Reformer hakkı PT için kullanılamaz.";
        if (account.MonthlyLimit is { } monthlyLimit)
        {
            var existing = await dbContext.PersonalTrainingSessions.AsNoTracking()
                .Where(session => session.MemberProfileId == member.Id && session.Id != excludedSessionId &&
                    session.Status != PersonalTrainingSessionStatus.Cancelled &&
                    ((session.LessonAccount != null && session.LessonAccount.PeriodId == account.PeriodId) ||
                     (session.LessonAccountId == null && (!member.MembershipStartsAtUtc.HasValue || session.StartsAtUtc >= member.MembershipStartsAtUtc) &&
                      (!member.MembershipEndsAtUtc.HasValue || session.StartsAtUtc < member.MembershipEndsAtUtc))))
                .Select(session => session.StartsAtUtc).ToListAsync();
            foreach (var month in starts.GroupBy(start => (ClubTime.ToLocal(start).Year, ClubTime.ToLocal(start).Month)))
                if (existing.Count(start => (ClubTime.ToLocal(start).Year, ClubTime.ToLocal(start).Month) == month.Key) + month.Count() > monthlyLimit)
                    return $"{month.Key.Month:00}.{month.Key.Year} ayında paket en fazla {monthlyLimit} birebir ders içeriyor.";
        }
        return null;
    }

    public Task<(bool Succeeded, string Message)> CreateAsync(
        int trainerId, int memberProfileId, DateTime startsAtUtc, int durationMinutes, string? note) =>
        CreateCoreAsync(
            trainerId,
            memberProfileId,
            startsAtUtc,
            durationMinutes,
            note,
            allowUnassignedMember: false,
            allowPastDates: false);

    public Task<(bool Succeeded, string Message)> CreateByAdminAsync(
        int trainerId, int memberProfileId, DateTime startsAtUtc, int durationMinutes, string? note) =>
        CreateCoreAsync(
            trainerId,
            memberProfileId,
            startsAtUtc,
            durationMinutes,
            note,
            allowUnassignedMember: true,
            allowPastDates: true);

    private async Task<(bool Succeeded, string Message)> CreateCoreAsync(
        int trainerId, int memberProfileId, DateTime startsAtUtc, int durationMinutes, string? note,
        bool allowUnassignedMember, bool allowPastDates)
    {
        try { return await CreateLockedAsync(trainerId, memberProfileId, startsAtUtc, durationMinutes, note, allowUnassignedMember, allowPastDates); }
        catch (DbUpdateException) { return (false, "Takvim başka bir işlemle değişti. Sayfayı yenileyip tekrar deneyin."); }
        catch (Exception exception) when (exception.GetBaseException() is Npgsql.PostgresException { SqlState: "40001" or "40P01" })
        { return (false, "Takvim başka bir işlemle değişti. Sayfayı yenileyip tekrar deneyin."); }
    }

    private async Task<(bool Succeeded, string Message)> CreateLockedAsync(
        int trainerId, int memberProfileId, DateTime startsAtUtc, int durationMinutes, string? note,
        bool allowUnassignedMember, bool allowPastDates)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        await TrainerScheduleGuard.LockAsync(dbContext, trainerId);
        await MemberCreditGuard.LockAsync(dbContext, memberProfileId);
        if (!allowPastDates && startsAtUtc <= DateTime.UtcNow)
            return (false, "Ders için gelecekte bir tarih ve saat seçmelisiniz.");
        if (!await dbContext.Trainers.AnyAsync(x => x.Id == trainerId && x.IsActive))
            return (false, "Aktif bir antrenör seçmelisiniz.");
        var member = await dbContext.MemberProfiles
            .Include(item => item.MembershipPackage)
            .Include(item => item.ServicePackageVariant)
            .FirstOrDefaultAsync(item => item.Id == memberProfileId &&
                (allowUnassignedMember || item.AssignedTrainerId == trainerId));
        if (member is null)
            return (false, allowUnassignedMember
                ? "Üye bulunamadı."
                : "Yalnızca size atanmış bir üyeye ders planlayabilirsiniz.");

        if (durationMinutes is < 15 or > 240)
            return (false, "Ders süresi 15 ile 240 dakika arasında olmalıdır.");

        var entitlementError = await ValidateEntitlementsAsync(
            member,
            [startsAtUtc],
            DateTime.UtcNow,
            durationMinutes,
            allowPastDates);
        if (entitlementError is not null)
            return (false, entitlementError);

        var endsAtUtc = startsAtUtc.AddMinutes(durationMinutes);
        var hasConflict = await dbContext.PersonalTrainingSessions.AnyAsync(item =>
            (item.TrainerId == trainerId || item.MemberProfileId == memberProfileId) && item.Status == PersonalTrainingSessionStatus.Scheduled &&
            item.StartsAtUtc < endsAtUtc && item.StartsAtUtc.AddMinutes(item.DurationMinutes) > startsAtUtc);
        if (hasConflict || await HasGroupConflictAsync(trainerId, startsAtUtc, endsAtUtc))
            return (false, "Bu saat aralığında başka bir ders veya ön görüşme bulunuyor.");

        dbContext.PersonalTrainingSessions.Add(new PersonalTrainingSession
        {
            LessonAccount = (await new CategoryCreditService(dbContext).PrepareAsync(member, true)).Accounts.Single(x => x.Category == LessonCategory.PersonalTraining),
            TrainerId = trainerId,
            MemberProfileId = memberProfileId,
            StartsAtUtc = startsAtUtc,
            RecurrenceId = Guid.NewGuid(),
            DurationMinutes = durationMinutes,
            Note = note?.Trim()
        });
        await dbContext.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return (true, "Ders takvime eklendi.");
    }

    public async Task<(bool Succeeded, string Message)> ChangeStatusAsync(
        int trainerId, int sessionId, PersonalTrainingSessionStatus status,
        DateTime? postponedStartsAtUtc, string changedByUserId, string? note)
    {
        try { return await ChangeStatusCoreAsync(trainerId, sessionId, status, postponedStartsAtUtc, changedByUserId, note); }
        catch (DbUpdateException) { return (false, "Ders başka bir işlemle değişti. Takvimi yenileyip tekrar deneyin."); }
        catch (InvalidOperationException exception) when (exception.GetBaseException() is Npgsql.PostgresException { SqlState: "40001" or "40P01" })
        { return (false, "Ders başka bir işlemle değişti. Takvimi yenileyip tekrar deneyin."); }
        catch (Npgsql.PostgresException exception) when (exception.SqlState is "40001" or "40P01")
        { return (false, "Ders başka bir işlemle değişti. Takvimi yenileyip tekrar deneyin."); }
    }

    private async Task<(bool Succeeded, string Message)> ChangeStatusCoreAsync(
        int trainerId, int sessionId, PersonalTrainingSessionStatus status,
        DateTime? postponedStartsAtUtc, string changedByUserId, string? note)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        await TrainerScheduleGuard.LockAsync(dbContext, trainerId);
        var session = await dbContext.PersonalTrainingSessions
            .Include(item => item.MemberProfile)
            .ThenInclude(item => item.MembershipPackage)
            .Include(item => item.MemberProfile)
            .ThenInclude(item => item.ServicePackageVariant)
            .FirstOrDefaultAsync(item => item.Id == sessionId && item.TrainerId == trainerId);
        if (session is null)
            return (false, "Ders bulunamadı.");
        if (session.Status != PersonalTrainingSessionStatus.Scheduled)
            return (false, "Sonuçlandırılmış bir ders yeniden değiştirilemez.");

        var previousStart = session.StartsAtUtc;
        var historyStatus = status;
        var now = DateTime.UtcNow;
        if (status is PersonalTrainingSessionStatus.Completed or PersonalTrainingSessionStatus.NoShow && session.StartsAtUtc > now)
            return (false, "Henüz başlamamış bir ders \"Tamamlandı\" veya \"Katılmadı\" olarak işaretlenemez.");

        if (status == PersonalTrainingSessionStatus.Postponed)
        {
            if (session.LessonAccount is { } source && source.PeriodId != session.MemberProfile.LessonPeriodId)
                return (false, "Eski paket dönemine ait ders yeni döneme taşınamaz. Dersi iptal edip yeni paketten planlayın.");
            if (postponedStartsAtUtc is null || postponedStartsAtUtc <= DateTime.UtcNow)
                return (false, "Erteleme için ileri bir tarih ve saat seçmelisiniz.");

            var entitlementError = await ValidateEntitlementsAsync(session.MemberProfile,
                [postponedStartsAtUtc.Value], DateTime.UtcNow, session.DurationMinutes,
                excludedSessionId: session.Id);
            if (entitlementError is not null)
                return (false, entitlementError);

            var newEnd = postponedStartsAtUtc.Value.AddMinutes(session.DurationMinutes);
            var hasConflict = await dbContext.PersonalTrainingSessions.AnyAsync(item =>
                item.Id != session.Id && (item.TrainerId == trainerId || item.MemberProfileId == session.MemberProfileId) &&
                item.Status == PersonalTrainingSessionStatus.Scheduled &&
                item.StartsAtUtc < newEnd &&
                item.StartsAtUtc.AddMinutes(item.DurationMinutes) > postponedStartsAtUtc.Value);
            if (hasConflict || await HasGroupConflictAsync(trainerId, postponedStartsAtUtc.Value, newEnd))
                return (false, "Yeni saat aralığında başka bir ders veya ön görüşme bulunuyor.");

            if (session.RecurrenceId is null)
            {
                var recurrences = new CalendarRecurrenceResolver(dbContext);
                await recurrences.PersonalIdsAsync(session, NO23.Web.ViewModels.Admin.CalendarBulkScope.SelectedLessons, DateTime.UtcNow);
                await recurrences.AssignLegacyIdentitiesAsync();
            }
            session.StartsAtUtc = postponedStartsAtUtc.Value;
            session.Status = PersonalTrainingSessionStatus.Scheduled;
            session.UpdatedAtUtc = now;
            session.History.Add(new PersonalTrainingSessionHistory
            {
                PreviousStatus = PersonalTrainingSessionStatus.Scheduled,
                NewStatus = historyStatus,
                PreviousStartsAtUtc = previousStart,
                NewStartsAtUtc = session.StartsAtUtc,
                Note = note?.Trim(),
                ChangedByUserId = changedByUserId,
                ChangedAtUtc = now
            });
        }
        else if (status is PersonalTrainingSessionStatus.Completed or
                 PersonalTrainingSessionStatus.Cancelled or
                 PersonalTrainingSessionStatus.NoShow)
        {
            if (status != PersonalTrainingSessionStatus.Cancelled && session.LessonAccount is null)
            {
                var allocation = await new CategoryCreditService(dbContext).PrepareAsync(session.MemberProfile, true);
                if (allocation.Error is not null) return (false, allocation.Error);
                session.LessonAccount = allocation.Accounts.Single(x => x.Category == LessonCategory.PersonalTraining);
            }
            if (status != PersonalTrainingSessionStatus.Cancelled && !session.CreditConsumed && session.LessonAccount?.Remaining <= 0)
                return (false, "Bu dersin kaynak PT hesabında yeterli hak yok. Kategori dağılımını kontrol edin.");
            PersonalTrainingSessionFinalizer.Apply(session, status, changedByUserId, note, now);
        }
        else
        {
            return (false, "Geçersiz ders durumu.");
        }

        await dbContext.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return (true, status == PersonalTrainingSessionStatus.Postponed
            ? "Ders ertelendi; ders hakkı düşülmedi." : "Ders durumu güncellendi.");
    }
    private async Task<bool> HasGroupConflictAsync(int trainerId, DateTime start, DateTime end) =>
        await TrainerScheduleGuard.HasAppointmentAsync(dbContext, trainerId, start, end) ||
        await dbContext.ClassSessions.AnyAsync(x => x.GroupClass.IsActive && x.GroupClass.TrainerId == trainerId &&
            x.Status == ClassSessionStatus.Scheduled && x.StartsAtUtc < end &&
            x.StartsAtUtc.AddMinutes(x.GroupClass.DurationMinutes) > start);
}
