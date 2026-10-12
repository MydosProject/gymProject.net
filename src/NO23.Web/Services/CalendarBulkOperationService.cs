using System.Data;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Services;

public sealed class CalendarBulkOperationService(ApplicationDbContext db)
{
    public async Task<(bool Succeeded, string Message)> ApplyAsync(CalendarBulkInput input, string userId, int? trainerScope = null)
    {
        try { return await ApplyCoreAsync(input, userId, trainerScope); }
        catch (DbUpdateException) { return (false, "Dersler başka bir işlemle değişti. Takvimi yenileyip tekrar deneyin."); }
        catch (Npgsql.PostgresException exception) when (exception.SqlState is "40001" or "40P01")
        { return (false, "Dersler başka bir işlemle değişti. Takvimi yenileyip tekrar deneyin."); }
    }

    private async Task<(bool Succeeded, string Message)> ApplyCoreAsync(CalendarBulkInput input, string userId, int? trainerScope)
    {
        if (!Enum.IsDefined(input.Operation) || string.IsNullOrWhiteSpace(userId)) return (false, "Geçerli bir toplu işlem seçin.");
        if (input.Operation == CalendarBulkOperation.UpdateTime && input.Time is null) return (false, "Yeni ders saatini girin.");
        if (input.PersonalIds.Length + input.GroupIds.Length > 500 || input.PersonalIds.Any(id => id <= 0) || input.GroupIds.Any(id => id <= 0))
            return (false, "Bir işlemde en fazla 500 geçerli ders seçebilirsiniz.");
        if (input.AllMemberLessons && (input.MemberId is null || input.From == default || input.Until < input.From ||
            input.From.Year is < 1900 or > 2100 || input.Until > input.From.AddYears(2)))
            return (false, "Üye ve en fazla iki yıllık geçerli tarih aralığı seçin.");
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var effectiveTrainerScope = trainerScope ?? input.TrainerId;
        var personalIds = input.PersonalIds.Distinct().ToArray();
        var groupIds = input.GroupIds.Distinct().ToArray();
        if (input.AllMemberLessons)
        {
            var fromUtc = ClubTime.ToUtc(input.From.ToDateTime(TimeOnly.MinValue));
            var untilUtc = ClubTime.ToUtc(input.Until.AddDays(1).ToDateTime(TimeOnly.MinValue));
            personalIds = await db.PersonalTrainingSessions.Where(item => item.MemberProfileId == input.MemberId &&
                item.Status == PersonalTrainingSessionStatus.Scheduled && item.StartsAtUtc >= fromUtc && item.StartsAtUtc < untilUtc &&
                (!effectiveTrainerScope.HasValue || item.TrainerId == effectiveTrainerScope)).Select(item => item.Id).Take(501).ToArrayAsync();
        }
        if (personalIds.Length + groupIds.Length is 0 or > 500) return (false, "İşlem yapılacak 1–500 ders seçin.");
        var personal = await db.PersonalTrainingSessions.Include(item => item.History)
            .Include(item => item.MemberProfile).ThenInclude(item => item.MembershipPackage)
            .Where(item => personalIds.Contains(item.Id)).AsSplitQuery().ToListAsync();
        var groups = await db.ClassSessions.Include(item => item.GroupClass).ThenInclude(item => item.Trainer)
            .Include(item => item.Reservations).ThenInclude(item => item.MemberProfile).ThenInclude(item => item.MembershipPackage)
            .Where(item => groupIds.Contains(item.Id)).AsSplitQuery().ToListAsync();
        if (personal.Count != personalIds.Length || groups.Count != groupIds.Length ||
            personal.Any(item => item.Status != PersonalTrainingSessionStatus.Scheduled ||
                (effectiveTrainerScope.HasValue && item.TrainerId != effectiveTrainerScope) ||
                (input.MemberId.HasValue && item.MemberProfileId != input.MemberId)) ||
            groups.Any(item => item.Status != ClassSessionStatus.Scheduled || !item.GroupClass.IsActive ||
                (effectiveTrainerScope.HasValue && item.GroupClass.TrainerId != effectiveTrainerScope) ||
                (input.MemberId.HasValue && !item.Reservations.Any(reservation => reservation.MemberProfileId == input.MemberId && reservation.Status == ClassReservationStatus.Reserved))))
            return (false, "Yalnızca seçilen üyeye / yetkinize ait planlanmış derslerde toplu işlem yapılabilir.");

        var now = DateTime.UtcNow;
        if (input.Operation == CalendarBulkOperation.Delete)
        {
            foreach (var session in personal)
            {
                if (session.CreditConsumed && !MemberPackageEntitlement.HasUnlimitedClassAccess(session.MemberProfile))
                {
                    session.MemberProfile.RemainingClassCredits++;
                    session.MemberProfile.UpdatedAtUtc = now;
                }
                db.PersonalTrainingSessionHistories.RemoveRange(session.History);
            }
            foreach (var session in groups)
                ClassReservationService.CancelSessionReservations(session, now, "Ders seansı toplu işlemle silindi.");
            db.ClassReservations.RemoveRange(groups.SelectMany(item => item.Reservations));
            db.ClassSessions.RemoveRange(groups);
            db.PersonalTrainingSessions.RemoveRange(personal);
        }
        else
        {
            var proposed = personal.Select(item => Slot.ForPersonal(item, AtNewTime(item.StartsAtUtc, input.Time!.Value)))
                .Concat(groups.Select(item => Slot.ForGroup(item, AtNewTime(item.StartsAtUtc, input.Time!.Value)))).ToList();
            if (proposed.Any(item => item.Start <= now)) return (false, "Yeni ders saati gelecekte olmalıdır.");
            foreach (var slot in proposed)
            {
                var members = slot.Personal is { } lesson ? new[] { lesson.MemberProfile } :
                    slot.Group!.Reservations.Where(item => item.Status == ClassReservationStatus.Reserved).Select(item => item.MemberProfile);
                if (members.Any(member => (member.MembershipStartsAtUtc.HasValue && slot.Start < member.MembershipStartsAtUtc) ||
                    (member.MembershipEndsAtUtc.HasValue && slot.End > member.MembershipEndsAtUtc)))
                    return (false, "Yeni saat, katılımcıların paket başlangıç / bitiş sınırlarının içinde olmalıdır.");
            }
            var trainers = proposed.Select(item => item.TrainerId).Distinct().ToArray();
            var memberIds = proposed.SelectMany(item => item.MemberIds).Distinct().ToArray();
            var min = proposed.Min(item => item.Start).AddDays(-1);
            var max = proposed.Max(item => item.End).AddDays(1);
            var otherPersonal = await db.PersonalTrainingSessions.AsNoTracking().Where(item =>
                !personalIds.Contains(item.Id) && item.Status == PersonalTrainingSessionStatus.Scheduled &&
                item.StartsAtUtc >= min && item.StartsAtUtc < max && (trainers.Contains(item.TrainerId) || memberIds.Contains(item.MemberProfileId))).ToListAsync();
            var otherGroups = await db.ClassSessions.AsNoTracking().Include(item => item.GroupClass).Include(item => item.Reservations)
                .Where(item => !groupIds.Contains(item.Id) && item.Status == ClassSessionStatus.Scheduled && item.GroupClass.IsActive &&
                    item.StartsAtUtc >= min && item.StartsAtUtc < max && (trainers.Contains(item.GroupClass.TrainerId) ||
                        item.Reservations.Any(reservation => reservation.Status == ClassReservationStatus.Reserved && memberIds.Contains(reservation.MemberProfileId))))
                .AsSplitQuery().ToListAsync();
            var existing = otherPersonal.Select(item => Slot.ForPersonal(item, item.StartsAtUtc))
                .Concat(otherGroups.Select(item => Slot.ForGroup(item, item.StartsAtUtc))).ToList();
            for (var index = 0; index < proposed.Count; index++)
            {
                var slot = proposed[index];
                if (existing.Any(other => Conflicts(slot, other)) || proposed.Take(index).Any(other => Conflicts(slot, other)))
                    return (false, "Yeni saat eğitmenin veya katılımcının başka dersiyle çakışıyor. Hiçbir ders değiştirilmedi.");
            }
            foreach (var slot in proposed)
            {
                if (slot.Personal is { } session)
                {
                    if (session.StartsAtUtc == slot.Start) continue;
                    session.History.Add(new PersonalTrainingSessionHistory {
                        PreviousStatus = session.Status, NewStatus = PersonalTrainingSessionStatus.Postponed, PreviousStartsAtUtc = session.StartsAtUtc,
                        NewStartsAtUtc = slot.Start, ChangedByUserId = userId, Note = "Toplu saat güncellemesi"
                    });
                    session.StartsAtUtc = slot.Start;
                    session.UpdatedAtUtc = now;
                }
                else { slot.Group!.StartsAtUtc = slot.Start; slot.Group.UpdatedAtUtc = now; }
            }
        }
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return (true, $"{personal.Count + groups.Count} ders " + (input.Operation == CalendarBulkOperation.Delete ? "silindi; ders hakları korundu." : "saati güncellendi; ders tarihleri ve hakları korundu."));
    }

    private static DateTime AtNewTime(DateTime start, TimeOnly time) => ClubTime.ToUtc(ClubTime.ToLocal(start).Date.Add(time.ToTimeSpan()));
    private static bool Conflicts(Slot first, Slot second) => first.Start < second.End && second.Start < first.End &&
        (first.TrainerId == second.TrainerId || first.MemberIds.Intersect(second.MemberIds).Any());
    private sealed record Slot(DateTime Start, int Duration, int TrainerId, int[] MemberIds, PersonalTrainingSession? Personal, ClassSession? Group)
    {
        public DateTime End => Start.AddMinutes(Duration);
        public static Slot ForPersonal(PersonalTrainingSession item, DateTime start) => new(start, item.DurationMinutes, item.TrainerId, [item.MemberProfileId], item, null);
        public static Slot ForGroup(ClassSession item, DateTime start) => new(start, item.GroupClass.DurationMinutes, item.GroupClass.TrainerId,
            item.Reservations.Where(reservation => reservation.Status == ClassReservationStatus.Reserved).Select(reservation => reservation.MemberProfileId).Distinct().ToArray(), null, item);
    }
}
