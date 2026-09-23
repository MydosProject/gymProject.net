using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Web.Services;

public class PersonalTrainingCalendarService(ApplicationDbContext dbContext)
{
    public Task<(bool Succeeded, string Message)> CreateWeeklyAsync(
        int trainerId, WeeklyPersonalSessionInput input) =>
        CreateWeeklyCoreAsync(trainerId, input, allowUnassignedMember: false);

    public Task<(bool Succeeded, string Message)> CreateWeeklyByAdminAsync(
        int trainerId, WeeklyPersonalSessionInput input) =>
        CreateWeeklyCoreAsync(trainerId, input, allowUnassignedMember: true);

    private async Task<(bool Succeeded, string Message)> CreateWeeklyCoreAsync(
        int trainerId, WeeklyPersonalSessionInput input, bool allowUnassignedMember)
    {
        if (input.Weeks is < 1 or > 52 || input.Days.Length == 0 ||
            input.Days.Any(day => !Enum.IsDefined(day)) ||
            input.DurationMinutes is < 15 or > 240)
            return (false, "Günleri, hafta sayısını ve ders süresini kontrol edin.");

        var trainerExists = await dbContext.Trainers.AnyAsync(trainer =>
            trainer.Id == trainerId && trainer.IsActive);
        if (!trainerExists)
            return (false, "Aktif bir antrenör seçmelisiniz.");

        var member = await dbContext.MemberProfiles
            .Include(profile => profile.MembershipPackage)
            .Include(profile => profile.ServicePackageVariant)
            .FirstOrDefaultAsync(profile => profile.Id == input.MemberProfileId &&
                (allowUnassignedMember || profile.AssignedTrainerId == trainerId));
        if (member is null)
            return (false, allowUnassignedMember
                ? "Üye bulunamadı."
                : "Yalnızca size atanmış bir üyeye ders planlayabilirsiniz.");

        var starts = Enumerable.Range(0, input.Weeks)
            .SelectMany(week => input.Days.Distinct().Select(day =>
                ClubTime.ToUtc(ClubTime.Monday(input.Week)
                    .AddDays(week * 7 + ((int)day + 6) % 7)
                    .Add(input.Time.ToTimeSpan()))))
            .OrderBy(start => start).ToList();
        var nowUtc = DateTime.UtcNow;
        if (starts.Any(start => start <= nowUtc))
            return (false, "Tüm birebir dersler gelecekte olmalı.");

        var entitlementError = await ValidateEntitlementsAsync(member, starts, nowUtc);
        if (entitlementError is not null)
            return (false, entitlementError);

        var windowStart = starts[0].AddDays(-1);
        var windowEnd = starts[^1].AddDays(1);
        var personal = await dbContext.PersonalTrainingSessions.AsNoTracking()
            .Where(session => (session.TrainerId == trainerId || session.MemberProfileId == member.Id) &&
                session.Status == PersonalTrainingSessionStatus.Scheduled &&
                session.StartsAtUtc >= windowStart && session.StartsAtUtc < windowEnd)
            .Select(session => new { session.StartsAtUtc, session.DurationMinutes })
            .ToListAsync();
        var groups = await dbContext.ClassSessions.AsNoTracking()
            .Where(session => session.GroupClass.TrainerId == trainerId &&
                session.Status == ClassSessionStatus.Scheduled &&
                session.StartsAtUtc >= windowStart && session.StartsAtUtc < windowEnd)
            .Select(session => new { session.StartsAtUtc, session.GroupClass.DurationMinutes })
            .ToListAsync();
        foreach (var start in starts)
        {
            var end = start.AddMinutes(input.DurationMinutes);
            if (personal.Any(session => session.StartsAtUtc < end &&
                    session.StartsAtUtc.AddMinutes(session.DurationMinutes) > start) ||
                groups.Any(session => session.StartsAtUtc < end &&
                    session.StartsAtUtc.AddMinutes(session.DurationMinutes) > start))
                return (false, $"{ClubTime.ToLocal(start):dd.MM.yyyy HH:mm} saatinde eğitmen veya üyenin başka dersi var. Hiçbir ders eklenmedi.");
        }

        dbContext.PersonalTrainingSessions.AddRange(starts.Select(start => new PersonalTrainingSession
        {
            TrainerId = trainerId,
            MemberProfileId = member.Id,
            StartsAtUtc = start,
            DurationMinutes = input.DurationMinutes,
            Note = input.Note?.Trim()
        }));
        await dbContext.SaveChangesAsync();
        return (true, $"{starts.Count} birebir ders haftalık programa eklendi.");
    }

    private async Task<string?> ValidateEntitlementsAsync(
        MemberProfile member, IReadOnlyList<DateTime> starts, DateTime nowUtc)
    {
        if (!MemberPackageEntitlement.IsActive(member, nowUtc) ||
            starts.Any(start => !MemberPackageEntitlement.IsActive(member, start)))
            return "Ders tarihleri üyenin aktif üyelik dönemi içinde olmalı.";

        var futureReserved = await dbContext.PersonalTrainingSessions.CountAsync(session =>
            session.MemberProfileId == member.Id &&
            session.Status == PersonalTrainingSessionStatus.Scheduled &&
            session.StartsAtUtc > nowUtc);
        if (!MemberPackageEntitlement.HasUnlimitedClassAccess(member) &&
            starts.Count + futureReserved > member.RemainingClassCredits)
            return "Planlanan ders sayısı üyenin kullanılabilir ders hakkını aşıyor.";

        if (member.ServicePackageVariant is { } variant)
        {
            if (variant.PersonalTrainingSessionCount <= 0)
                return "Üyenin seçili paketi birebir ders hakkı içermiyor.";

            if (variant.LessonsRenewMonthly)
            {
                var firstLocal = ClubTime.ToLocal(starts[0]);
                var lastLocal = ClubTime.ToLocal(starts[^1]);
                var firstMonth = ClubTime.ToUtc(new DateTime(firstLocal.Year, firstLocal.Month, 1));
                var lastMonthEnd = ClubTime.ToUtc(new DateTime(lastLocal.Year, lastLocal.Month, 1).AddMonths(1));
                var existing = await dbContext.PersonalTrainingSessions.AsNoTracking()
                    .Where(session => session.MemberProfileId == member.Id &&
                        session.StartsAtUtc >= firstMonth && session.StartsAtUtc < lastMonthEnd &&
                        (!member.MembershipStartsAtUtc.HasValue || session.StartsAtUtc >= member.MembershipStartsAtUtc.Value))
                    .Select(session => session.StartsAtUtc)
                    .ToListAsync();
                foreach (var month in starts.GroupBy(start =>
                    (ClubTime.ToLocal(start).Year, ClubTime.ToLocal(start).Month)))
                {
                    var scheduledInMonth = existing.Count(start =>
                        ClubTime.ToLocal(start).Year == month.Key.Year &&
                        ClubTime.ToLocal(start).Month == month.Key.Month);
                    if (scheduledInMonth + month.Count() > variant.PersonalTrainingSessionCount)
                        return $"{month.Key.Month:00}.{month.Key.Year} ayında paket en fazla " +
                            $"{variant.PersonalTrainingSessionCount} birebir ders içeriyor.";
                }
            }
            else
            {
                var used = await dbContext.PersonalTrainingSessions.CountAsync(session =>
                    session.MemberProfileId == member.Id &&
                    (!member.MembershipStartsAtUtc.HasValue || session.StartsAtUtc >= member.MembershipStartsAtUtc.Value));
                if (used + starts.Count > variant.PersonalTrainingSessionCount)
                    return "Planlanan dersler paketin birebir ders sayısını aşıyor.";
            }
        }
        return null;
    }

    public Task<(bool Succeeded, string Message)> CreateAsync(
        int trainerId, int memberProfileId, DateTime startsAtUtc, int durationMinutes, string? note) =>
        CreateCoreAsync(trainerId, memberProfileId, startsAtUtc, durationMinutes, note, allowUnassignedMember: false);

    public Task<(bool Succeeded, string Message)> CreateByAdminAsync(
        int trainerId, int memberProfileId, DateTime startsAtUtc, int durationMinutes, string? note) =>
        CreateCoreAsync(trainerId, memberProfileId, startsAtUtc, durationMinutes, note, allowUnassignedMember: true);

    private async Task<(bool Succeeded, string Message)> CreateCoreAsync(
        int trainerId, int memberProfileId, DateTime startsAtUtc, int durationMinutes, string? note,
        bool allowUnassignedMember)
    {
        if (startsAtUtc <= DateTime.UtcNow)
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

        var entitlementError = await ValidateEntitlementsAsync(member, [startsAtUtc], DateTime.UtcNow);
        if (entitlementError is not null)
            return (false, entitlementError);

        var endsAtUtc = startsAtUtc.AddMinutes(durationMinutes);
        var hasConflict = await dbContext.PersonalTrainingSessions.AnyAsync(item =>
            (item.TrainerId == trainerId || item.MemberProfileId == memberProfileId) && item.Status == PersonalTrainingSessionStatus.Scheduled &&
            item.StartsAtUtc < endsAtUtc && item.StartsAtUtc.AddMinutes(item.DurationMinutes) > startsAtUtc);
        if (hasConflict || await HasGroupConflictAsync(trainerId, startsAtUtc, endsAtUtc))
            return (false, "Bu saat aralığında başka bir dersiniz bulunuyor.");

        dbContext.PersonalTrainingSessions.Add(new PersonalTrainingSession
        {
            TrainerId = trainerId,
            MemberProfileId = memberProfileId,
            StartsAtUtc = startsAtUtc,
            DurationMinutes = durationMinutes,
            Note = note?.Trim()
        });
        await dbContext.SaveChangesAsync();
        return (true, "Ders takvime eklendi.");
    }

    public async Task<(bool Succeeded, string Message)> ChangeStatusAsync(
        int trainerId, int sessionId, PersonalTrainingSessionStatus status,
        DateTime? postponedStartsAtUtc, string changedByUserId, string? note)
    {
        var session = await dbContext.PersonalTrainingSessions
            .Include(item => item.MemberProfile)
            .ThenInclude(item => item.MembershipPackage)
            .FirstOrDefaultAsync(item => item.Id == sessionId && item.TrainerId == trainerId);
        if (session is null)
            return (false, "Ders bulunamadı.");
        if (session.Status != PersonalTrainingSessionStatus.Scheduled)
            return (false, "Sonuçlandırılmış bir ders yeniden değiştirilemez.");

        var previousStart = session.StartsAtUtc;
        var historyStatus = status;

        if (status == PersonalTrainingSessionStatus.Postponed)
        {
            if (postponedStartsAtUtc is null || postponedStartsAtUtc <= DateTime.UtcNow)
                return (false, "Erteleme için ileri bir tarih ve saat seçmelisiniz.");

            var newEnd = postponedStartsAtUtc.Value.AddMinutes(session.DurationMinutes);
            var hasConflict = await dbContext.PersonalTrainingSessions.AnyAsync(item =>
                item.Id != session.Id && (item.TrainerId == trainerId || item.MemberProfileId == session.MemberProfileId) &&
                item.Status == PersonalTrainingSessionStatus.Scheduled &&
                item.StartsAtUtc < newEnd &&
                item.StartsAtUtc.AddMinutes(item.DurationMinutes) > postponedStartsAtUtc.Value);
            if (hasConflict || await HasGroupConflictAsync(trainerId, postponedStartsAtUtc.Value, newEnd))
                return (false, "Yeni saat aralığında başka bir dersiniz bulunuyor.");

            session.StartsAtUtc = postponedStartsAtUtc.Value;
            session.Status = PersonalTrainingSessionStatus.Scheduled;
        }
        else if (status is PersonalTrainingSessionStatus.Completed or
                 PersonalTrainingSessionStatus.Cancelled or
                 PersonalTrainingSessionStatus.NoShow)
        {
            session.Status = status;
            if (!session.CreditConsumed)
            {
                if (!MemberPackageEntitlement.HasUnlimitedClassAccess(session.MemberProfile))
                    session.MemberProfile.RemainingClassCredits--;
                session.CreditConsumed = true;
                session.MemberProfile.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
        else
        {
            return (false, "Geçersiz ders durumu.");
        }

        session.UpdatedAtUtc = DateTime.UtcNow;
        dbContext.PersonalTrainingSessionHistories.Add(new PersonalTrainingSessionHistory
        {
            PersonalTrainingSessionId = session.Id,
            PreviousStatus = PersonalTrainingSessionStatus.Scheduled,
            NewStatus = historyStatus,
            PreviousStartsAtUtc = previousStart,
            NewStartsAtUtc = session.StartsAtUtc,
            Note = note?.Trim(),
            ChangedByUserId = changedByUserId
        });
        await dbContext.SaveChangesAsync();
        return (true, status == PersonalTrainingSessionStatus.Postponed
            ? "Ders ertelendi; ders hakkı düşülmedi." : "Ders durumu güncellendi.");
    }
    private Task<bool> HasGroupConflictAsync(int trainerId, DateTime start, DateTime end) =>
        dbContext.ClassSessions.AnyAsync(x => x.GroupClass.TrainerId == trainerId &&
            x.Status == ClassSessionStatus.Scheduled && x.StartsAtUtc < end &&
            x.StartsAtUtc.AddMinutes(x.GroupClass.DurationMinutes) > start);
}
