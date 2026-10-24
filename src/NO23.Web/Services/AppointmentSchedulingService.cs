using System.Data;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Services;

public sealed class AppointmentSchedulingService(ApplicationDbContext db)
{
    public Task<(bool Succeeded, string Message)> ScheduleAsync(AppointmentScheduleViewModel input, string userId) =>
        RunAsync(async () =>
        {
            await using var transaction = db.Database.IsRelational()
                ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
            var request = await db.AppointmentRequests.FirstOrDefaultAsync(x => x.Id == input.Id);
            if (request is null) return (false, "Randevu talebi bulunamadı.");
            if (input.ExpectedVersion != request.Version) return Stale();
            if (request.ScheduleStatus == AppointmentScheduleStatus.Completed)
                return (false, "Tamamlanmış görüşme yeniden planlanamaz.");
            if (input.DurationMinutes is < 15 or > 240 || input.Note?.Length > 1000 || string.IsNullOrWhiteSpace(userId))
                return (false, "Süreyi, işlem notunu ve kullanıcı bilgilerini kontrol edin.");
            if (input.StartsAt == default || input.StartsAt.Year is < 1900 or > 2100)
                return (false, "Geçerli bir tarih ve saat girin.");
            var start = ClubTime.ToUtc(input.StartsAt);
            if (start <= DateTime.UtcNow) return (false, "Görüşme için gelecekte bir tarih ve saat seçin.");
            await TrainerScheduleGuard.LockAsync(db, new[] { input.TrainerId, request.ScheduledTrainerId ?? input.TrainerId });
            if (!await db.Trainers.AnyAsync(x => x.Id == input.TrainerId && x.IsActive))
                return (false, "Aktif bir antrenör seçin.");
            var end = start.AddMinutes(input.DurationMinutes);
            if (await TrainerScheduleGuard.HasAppointmentAsync(db, input.TrainerId, start, end, request.Id) ||
                await db.PersonalTrainingSessions.AnyAsync(x => x.TrainerId == input.TrainerId &&
                    x.Status == PersonalTrainingSessionStatus.Scheduled && x.StartsAtUtc < end &&
                    x.StartsAtUtc.AddMinutes(x.DurationMinutes) > start) ||
                await db.ClassSessions.AnyAsync(x => x.GroupClass.TrainerId == input.TrainerId && x.GroupClass.IsActive &&
                    x.Status == ClassSessionStatus.Scheduled && x.StartsAtUtc < end &&
                    x.StartsAtUtc.AddMinutes(x.GroupClass.DurationMinutes) > start))
                return (false, "Antrenörün bu saatte dersi veya ön görüşmesi var. Başka bir zaman seçin.");

            AddHistory(request, start, input.TrainerId, input.DurationMinutes, AppointmentScheduleStatus.Scheduled, userId, input.Note);
            request.ScheduledTrainerId = input.TrainerId;
            request.ScheduledAtUtc = start;
            request.DurationMinutes = input.DurationMinutes;
            request.ScheduleStatus = AppointmentScheduleStatus.Scheduled;
            request.Status = ServicePackageApplicationStatus.Approved;
            Touch(request);
            await db.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
            return (true, "Ön görüşme takvime planlandı. Ders hakkı kullanılmadı.");
        });

    public Task<(bool Succeeded, string Message)> FinishAsync(int id, Guid expectedVersion,
        AppointmentScheduleStatus status, string userId, string? note = null) => RunAsync(async () =>
    {
        if (status is not (AppointmentScheduleStatus.Cancelled or AppointmentScheduleStatus.Completed) ||
            string.IsNullOrWhiteSpace(userId) || note?.Length > 1000)
            return (false, "Geçerli bir işlem seçin.");
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var request = await db.AppointmentRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (request is null) return (false, "Görüşme bulunamadı.");
        if (request.Version != expectedVersion) return Stale();
        if (request.ScheduleStatus != AppointmentScheduleStatus.Scheduled)
            return (false, "Yalnızca planlanmış görüşmeler sonuçlandırılabilir.");
        if (status == AppointmentScheduleStatus.Completed &&
            request.ScheduledAtUtc!.Value.AddMinutes(request.DurationMinutes) > DateTime.UtcNow)
            return (false, "Görüşme bitmeden tamamlandı olarak işaretlenemez.");
        AddHistory(request, request.ScheduledAtUtc, request.ScheduledTrainerId, request.DurationMinutes, status, userId, note);
        request.ScheduleStatus = status;
        request.Status = ServicePackageApplicationStatus.Closed;
        Touch(request);
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return (true, status == AppointmentScheduleStatus.Cancelled ? "Ön görüşme iptal edildi ve takvimden kaldırıldı." : "Ön görüşme tamamlandı.");
    });

    public Task<(bool Succeeded, string Message)> UpdateRequestStatusAsync(int id,
        ServicePackageApplicationStatus status, string userId) => RunAsync(async () =>
    {
        if (!Enum.IsDefined(status) || string.IsNullOrWhiteSpace(userId)) return (false, "Geçerli bir talep durumu ve kullanıcı bilgisi gerekir.");
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var request = await db.AppointmentRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (request is null) return (false, "Randevu talebi bulunamadı.");
        if (status == ServicePackageApplicationStatus.Closed && request.ScheduleStatus == AppointmentScheduleStatus.Scheduled)
        {
            AddHistory(request, request.ScheduledAtUtc, request.ScheduledTrainerId, request.DurationMinutes,
                AppointmentScheduleStatus.Cancelled, userId, "Randevu talebi kapatıldı.");
            request.ScheduleStatus = AppointmentScheduleStatus.Cancelled;
        }
        request.Status = status;
        Touch(request);
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return (true, "Randevu talebinin durumu güncellendi.");
    });

    private static void Touch(AppointmentRequest request)
    {
        request.Version = Guid.NewGuid();
        request.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void AddHistory(AppointmentRequest request, DateTime? start, int? trainerId,
        int duration, AppointmentScheduleStatus status, string userId, string? note) => request.ScheduleHistory.Add(new()
    {
        PreviousStartsAtUtc = request.ScheduledAtUtc, StartsAtUtc = start,
        PreviousTrainerId = request.ScheduledTrainerId, TrainerId = trainerId,
        PreviousDurationMinutes = request.DurationMinutes, DurationMinutes = duration,
        PreviousStatus = request.ScheduleStatus, Status = status, ChangedByUserId = userId, Note = note?.Trim()
    });

    private static (bool, string) Stale() => (false, "Görüşme başka bir işlemle değişti. Sayfayı yenileyip tekrar deneyin.");

    private static async Task<(bool Succeeded, string Message)> RunAsync(Func<Task<(bool, string)>> operation)
    {
        try { return await operation(); }
        catch (DbUpdateConcurrencyException) { return Stale(); }
        catch (Exception exception) when (exception.GetBaseException() is Npgsql.PostgresException { SqlState: "40001" or "40P01" })
        { return Stale(); }
    }
}
