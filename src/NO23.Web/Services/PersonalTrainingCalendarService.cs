using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class PersonalTrainingCalendarService(ApplicationDbContext dbContext)
{
    public async Task<(bool Succeeded, string Message)> CreateAsync(
        int trainerId, int memberProfileId, DateTime startsAtUtc, int durationMinutes, string? note)
    {
        var member = await dbContext.MemberProfiles
            .Include(item => item.MembershipPackage)
            .FirstOrDefaultAsync(item => item.Id == memberProfileId && item.AssignedTrainerId == trainerId);
        if (member is null)
            return (false, "Yalnızca size atanmış bir üyeye ders planlayabilirsiniz.");

        if (member.MembershipPackage.WeeklyClassLimit is not null && member.RemainingClassCredits <= 0)
            return (false, "Üyenin kalan ders hakkı bulunmuyor.");

        if (durationMinutes is < 15 or > 240)
            return (false, "Ders süresi 15 ile 240 dakika arasında olmalıdır.");

        var endsAtUtc = startsAtUtc.AddMinutes(durationMinutes);
        var hasConflict = await dbContext.PersonalTrainingSessions.AnyAsync(item =>
            item.TrainerId == trainerId && item.Status == PersonalTrainingSessionStatus.Scheduled &&
            item.StartsAtUtc < endsAtUtc && item.StartsAtUtc.AddMinutes(item.DurationMinutes) > startsAtUtc);
        if (hasConflict)
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
                item.Id != session.Id && item.TrainerId == trainerId &&
                item.Status == PersonalTrainingSessionStatus.Scheduled &&
                item.StartsAtUtc < newEnd &&
                item.StartsAtUtc.AddMinutes(item.DurationMinutes) > postponedStartsAtUtc.Value);
            if (hasConflict)
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
                if (session.MemberProfile.MembershipPackage.WeeklyClassLimit is not null)
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
}
