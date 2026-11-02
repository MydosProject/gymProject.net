using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Services;

public sealed class MemberPersonalScheduleService(ApplicationDbContext db)
{
    public async Task<IReadOnlyList<MemberPersonalSessionViewModel>> UpcomingAsync(int memberId, DateTime nowUtc) =>
        await db.PersonalTrainingSessions.AsNoTracking()
            .Where(x => x.MemberProfileId == memberId && x.StartsAtUtc >= nowUtc &&
                (x.LessonAccount == null || x.LessonAccount.PeriodId == x.MemberProfile.LessonPeriodId) &&
                (x.Status == PersonalTrainingSessionStatus.Scheduled || x.Status == PersonalTrainingSessionStatus.Postponed) &&
                (!x.MemberProfile.MembershipStartsAtUtc.HasValue || x.StartsAtUtc >= x.MemberProfile.MembershipStartsAtUtc.Value) &&
                (!x.MemberProfile.MembershipEndsAtUtc.HasValue || x.StartsAtUtc < x.MemberProfile.MembershipEndsAtUtc.Value))
            .OrderBy(x => x.StartsAtUtc).ThenBy(x => x.Id)
            .Select(x => new MemberPersonalSessionViewModel(x.Id, x.StartsAtUtc, x.DurationMinutes,
                x.Trainer.FirstName + " " + x.Trainer.LastName)).ToListAsync();
}
