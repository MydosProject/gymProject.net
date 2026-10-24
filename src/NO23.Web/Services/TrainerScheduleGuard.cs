using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public static class TrainerScheduleGuard
{
    public static async Task LockAsync(ApplicationDbContext db, params int[] trainerIds)
    {
        if (!db.Database.IsRelational()) return;
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Takvim kilidi bir işlem içinde alınmalıdır.");
        // Also advance the PostgreSQL row version: a serializable waiter must retry
        // instead of checking conflicts against a snapshot from before the last booking.
        foreach (var id in trainerIds.Distinct().Order())
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Trainers\" SET \"UpdatedAtUtc\" = \"UpdatedAtUtc\" WHERE \"Id\" = {id}");
    }

    public static Task<bool> HasAppointmentAsync(ApplicationDbContext db, int trainerId, DateTime start, DateTime end,
        int? excludedRequestId = null) => db.AppointmentRequests.AnyAsync(x => x.Id != excludedRequestId &&
            x.ScheduledTrainerId == trainerId && x.ScheduleStatus == AppointmentScheduleStatus.Scheduled &&
            x.ScheduledAtUtc < end && x.ScheduledAtUtc!.Value.AddMinutes(x.DurationMinutes) > start);
}
