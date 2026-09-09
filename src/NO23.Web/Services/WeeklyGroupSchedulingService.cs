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
        if (input.Weeks is < 1 or > 12 || input.Days.Length == 0 || input.Days.Any(x => !Enum.IsDefined(x)) || input.Capacity is < 1 or > 200)
            return (false, "Günleri, hafta sayısını ve kontenjanı kontrol et.");
        var group = await db.GroupClasses.Include(x => x.Trainer).FirstOrDefaultAsync(x => x.Id == input.GroupClassId && x.IsActive);
        if (group is null || !group.Trainer.IsActive) return (false, "Aktif bir ders ve antrenör seçmelisin.");
        var monday = ClubTime.Monday(input.Week);
        var starts = Enumerable.Range(0, input.Weeks).SelectMany(w => input.Days.Distinct().Select(day =>
            ClubTime.ToUtc(monday.AddDays(w * 7 + ((int)day + 6) % 7).Add(input.Time.ToTimeSpan())))).ToList();
        if (starts.Any(x => x <= DateTime.UtcNow)) return (false, "Tüm seanslar gelecekte olmalı. Başlangıç haftasını ve günleri kontrol et.");
        foreach (var start in starts)
        {
            var end = start.AddMinutes(group.DurationMinutes);
            if (await db.ClassSessions.AnyAsync(x => x.GroupClass.TrainerId == group.TrainerId && x.Status == ClassSessionStatus.Scheduled &&
                    x.StartsAtUtc < end && x.StartsAtUtc.AddMinutes(x.GroupClass.DurationMinutes) > start) ||
                await db.PersonalTrainingSessions.AnyAsync(x => x.TrainerId == group.TrainerId && x.Status == PersonalTrainingSessionStatus.Scheduled &&
                    x.StartsAtUtc < end && x.StartsAtUtc.AddMinutes(x.DurationMinutes) > start))
                return (false, $"{ClubTime.ToLocal(start):dd.MM.yyyy HH:mm} saatinde antrenörün başka dersi var. Hiçbir seans eklenmedi.");
        }
        db.ClassSessions.AddRange(starts.Select(start => new ClassSession
            { GroupClassId = group.Id, StartsAtUtc = start, CapacityOverride = input.Capacity, Status = ClassSessionStatus.Scheduled }));
        await db.SaveChangesAsync();
        return (true, $"{starts.Count} grup dersi haftalık takvime eklendi.");
    }
}
