using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Services;

public class WeeklyGroupSchedulingService(ApplicationDbContext db)
{
    public async Task<(bool Succeeded, string Message)> CreateAsync(WeeklyGroupInput input)
    {
        if (input.Weeks is < 1 or > 52 || input.Days.Length == 0 || input.Days.Any(x => !Enum.IsDefined(x)) || input.Capacity is < 1 or > 200)
            return (false, "Günleri, hafta sayısını ve kontenjanı kontrol et.");
        var group = await db.GroupClasses.Include(x => x.Trainer).FirstOrDefaultAsync(x => x.Id == input.GroupClassId && x.IsActive);
        if (group is null || !group.Trainer.IsActive) return (false, "Aktif bir ders ve antrenör seçmelisin.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync() : null;
        await TrainerScheduleGuard.LockAsync(db, group.TrainerId);
        var monday = ClubTime.Monday(input.Week);
        var starts = Enumerable.Range(0, input.Weeks).SelectMany(w => input.Days.Distinct().Select(day =>
            ClubTime.ToUtc(monday.AddDays(w * 7 + ((int)day + 6) % 7).Add(input.Time.ToTimeSpan()))))
            .OrderBy(start => start).ToList();
        if (starts.Any(x => x <= DateTime.UtcNow)) return (false, "Tüm seanslar gelecekte olmalı. Başlangıç haftasını ve günleri kontrol et.");
        var windowStart = starts[0].AddDays(-1);
        var windowEnd = starts[^1].AddDays(1);
        var existingGroups = await db.ClassSessions.AsNoTracking()
            .Where(x => x.GroupClass.IsActive && x.GroupClass.TrainerId == group.TrainerId &&
                x.Status == ClassSessionStatus.Scheduled &&
                x.StartsAtUtc >= windowStart && x.StartsAtUtc < windowEnd)
            .Select(x => new { x.StartsAtUtc, x.GroupClass.DurationMinutes })
            .ToListAsync();
        var existingPersonal = await db.PersonalTrainingSessions.AsNoTracking()
            .Where(x => x.TrainerId == group.TrainerId &&
                x.Status == PersonalTrainingSessionStatus.Scheduled &&
                x.StartsAtUtc >= windowStart && x.StartsAtUtc < windowEnd)
            .Select(x => new { x.StartsAtUtc, x.DurationMinutes })
            .ToListAsync();
        var appointments = await db.AppointmentRequests.AsNoTracking()
            .Where(x => x.ScheduledTrainerId == group.TrainerId && x.ScheduleStatus == AppointmentScheduleStatus.Scheduled &&
                x.ScheduledAtUtc >= windowStart && x.ScheduledAtUtc < windowEnd)
            .Select(x => new { x.ScheduledAtUtc, x.DurationMinutes }).ToListAsync();
        foreach (var start in starts)
        {
            var end = start.AddMinutes(group.DurationMinutes);
            if (existingGroups.Any(x => x.StartsAtUtc < end && x.StartsAtUtc.AddMinutes(x.DurationMinutes) > start) ||
                existingPersonal.Any(x => x.StartsAtUtc < end && x.StartsAtUtc.AddMinutes(x.DurationMinutes) > start) ||
                appointments.Any(x => x.ScheduledAtUtc < end && x.ScheduledAtUtc!.Value.AddMinutes(x.DurationMinutes) > start))
                return (false, $"{ClubTime.ToLocal(start):dd.MM.yyyy HH:mm} saatinde antrenörün dersi veya ön görüşmesi var. Hiçbir seans eklenmedi.");
        }
        var recurrenceIds = input.Days.Distinct().ToDictionary(day => day, _ => Guid.NewGuid());
        db.ClassSessions.AddRange(starts.Select(start => new ClassSession
            { GroupClassId = group.Id, StartsAtUtc = start, RecurrenceId = recurrenceIds[ClubTime.ToLocal(start).DayOfWeek],
                CapacityOverride = input.Capacity, Status = ClassSessionStatus.Scheduled }));
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return (true, $"{starts.Count} grup dersi haftalık takvime eklendi.");
    }
}
