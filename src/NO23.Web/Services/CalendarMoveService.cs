using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Services;

// Drag and drop / resize on the calendar. Each move goes through the same rules as the forms:
// a PT lesson is rescheduled like "Tarih / saat değiştir", a group lesson like a one-lesson bulk move.
public sealed class CalendarMoveService(ApplicationDbContext db, PersonalTrainingCalendarService personal)
{
    public async Task<(bool Succeeded, string Message)> MoveAsync(CalendarMoveInput input, string userId, int? trainerScope)
    {
        if (string.IsNullOrWhiteSpace(userId) || input.Id <= 0 || input.Start.Year is < 1900 or > 2100)
            return (false, "Taşınacak dersi ve yeni zamanı kontrol edin.");
        var startUtc = ClubTime.ToUtc(input.Start);
        if (input.Kind == CalendarMoveInput.Personal)
        {
            var session = await db.PersonalTrainingSessions.AsNoTracking().Where(x => x.Id == input.Id)
                .Select(x => new { x.TrainerId, x.StartsAtUtc, x.DurationMinutes }).FirstOrDefaultAsync();
            if (session is null || (trainerScope.HasValue && session.TrainerId != trainerScope))
                return (false, "Ders bulunamadı.");
            var duration = input.DurationMinutes ?? session.DurationMinutes;
            var note = duration == session.DurationMinutes ? "Takvimde sürükleyerek taşındı"
                : startUtc == session.StartsAtUtc ? $"Takvimde süre {session.DurationMinutes} → {duration} dk değiştirildi"
                : $"Takvimde taşındı; süre {session.DurationMinutes} → {duration} dk";
            var result = await personal.ChangeStatusAsync(session.TrainerId, input.Id, PersonalTrainingSessionStatus.Postponed,
                startUtc, userId, note, duration);
            if (!result.Succeeded) return result;
            return startUtc == session.StartsAtUtc
                ? (true, $"Ders süresi {duration} dakika olarak güncellendi; ders hakkı değişmedi.")
                : (true, "Ders yeni zamanına taşındı; ders hakkı değişmedi.");
        }
        if (input.Kind == CalendarMoveInput.Group)
        {
            var group = await db.ClassSessions.AsNoTracking().Where(x => x.Id == input.Id)
                .Select(x => new { x.GroupClass.DurationMinutes }).FirstOrDefaultAsync();
            if (group is null) return (false, "Ders bulunamadı.");
            if (input.DurationMinutes.HasValue && input.DurationMinutes != group.DurationMinutes)
                return (false, "Grup dersinin süresi ders tanımından gelir; takvimden değiştirilemez.");
            var result = await new CalendarBulkOperationService(db).ApplyAsync(new CalendarBulkInput
            {
                Operation = CalendarBulkOperation.UpdateTime, Scope = CalendarBulkScope.SelectedLessons, GroupIds = [input.Id],
                Date = DateOnly.FromDateTime(input.Start), Time = TimeOnly.FromDateTime(input.Start)
            }, userId, trainerScope);
            return result.Succeeded ? (true, "Grup dersi yeni zamanına taşındı; katılımcıların hakları korundu.") : result;
        }
        return (false, "Bu kayıt takvimden taşınamaz.");
    }
}

public static class CalendarMoveNotifier
{
    // Members always hear about the new time; the trainer only when someone else moved the lesson.
    public static async Task PublishAsync(ApplicationDbContext db, UserNotificationRealtimeService notifications,
        CalendarMoveInput input, bool notifyTrainer)
    {
        if (input.Kind == CalendarMoveInput.Personal)
        {
            var lesson = await db.PersonalTrainingSessions.AsNoTracking().Where(x => x.Id == input.Id)
                .Select(x => new
                {
                    x.StartsAtUtc, MemberUserId = x.MemberProfile.ApplicationUserId, TrainerUserId = x.Trainer.ApplicationUserId,
                    TrainerName = (x.Trainer.FirstName ?? "") + " " + (x.Trainer.LastName ?? ""),
                    MemberName = (x.MemberProfile.ApplicationUser.FirstName ?? "") + " " + (x.MemberProfile.ApplicationUser.LastName ?? "")
                })
                .FirstOrDefaultAsync();
            if (lesson is null) return;
            var when = ClubTime.ToLocal(lesson.StartsAtUtc).ToString("dd.MM.yyyy HH:mm");
            if (!string.IsNullOrWhiteSpace(lesson.MemberUserId))
                await notifications.CreateAndPublishAsync(lesson.MemberUserId, UserNotificationType.PersonalTrainingRescheduled,
                    "Birebir dersin değişti", $"{lesson.TrainerName.Trim()} ile birebir dersin {when} olarak güncellendi.",
                    "/Member/Reservations#personal-training", input.Id);
            if (notifyTrainer && !string.IsNullOrWhiteSpace(lesson.TrainerUserId))
                await notifications.CreateAndPublishAsync(lesson.TrainerUserId, UserNotificationType.PersonalTrainingRescheduled,
                    "Birebir dersin değişti", $"{lesson.MemberName.Trim()} ile birebir dersin {when} olarak güncellendi.",
                    "/Trainer/Calendar", input.Id);
            return;
        }

        var session = await db.ClassSessions.AsNoTracking().Where(x => x.Id == input.Id)
            .Select(x => new
            {
                x.StartsAtUtc, ClassName = x.GroupClass.Name, TrainerUserId = x.GroupClass.Trainer.ApplicationUserId,
                MemberUserIds = x.Reservations.Where(r => r.Status == ClassReservationStatus.Reserved)
                    .Select(r => r.MemberProfile.ApplicationUserId).Distinct().ToList()
            })
            .FirstOrDefaultAsync();
        if (session is null) return;
        var startsAt = ClubTime.ToLocal(session.StartsAtUtc);
        foreach (var memberUserId in session.MemberUserIds.Where(id => !string.IsNullOrWhiteSpace(id)))
            await notifications.CreateAndPublishAsync(memberUserId, UserNotificationType.GroupClassSessionChanged,
                "Ders saati değişti", $"{session.ClassName} dersinin tarih veya saati değiştirildi. Yeni zaman: {startsAt:dd.MM.yyyy HH:mm}.",
                "/Member/Reservations", input.Id);
        if (notifyTrainer && !string.IsNullOrWhiteSpace(session.TrainerUserId))
            await notifications.CreateAndPublishAsync(session.TrainerUserId, UserNotificationType.GroupClassSessionChanged,
                "Grup dersinin zamanı değiştirildi", $"{session.ClassName} dersinin yeni zamanı {startsAt:dd.MM.yyyy HH:mm} olarak güncellendi.",
                "/Trainer/Classes", input.Id);
    }
}
