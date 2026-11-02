using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public static class CategoryScheduleGuard
{
    public static async Task<string?> GroupMovesAsync(ApplicationDbContext db,
        IReadOnlyList<(ClassReservation Reservation, DateTime Start)> moves, int[] excludedSessionIds)
    {
        foreach (var memberMoves in moves.GroupBy(x => x.Reservation.MemberProfileId))
        {
            var member = memberMoves.First().Reservation.MemberProfile;
            if (memberMoves.Any(x => member.MembershipActivationPending ||
                (member.MembershipStartsAtUtc.HasValue && x.Start < member.MembershipStartsAtUtc) ||
                (member.MembershipEndsAtUtc.HasValue && x.Start.AddMinutes(x.Reservation.ClassSession.GroupClass.DurationMinutes) > member.MembershipEndsAtUtc)))
                return "Yeni ders tarihi katılımcının aktif paket dönemi içinde olmalı.";
            if (!member.LessonPeriodId.HasValue && member.ServicePackageVariant?.LessonsRenewMonthly != true) continue;
            var allocation = await new CategoryCreditService(db).PrepareAsync(member, false);
            if (allocation.Error is not null) return allocation.Error;
            foreach (var categoryMoves in memberMoves.GroupBy(x => x.Reservation.LessonAccount?.Category ?? x.Reservation.ClassSession.GroupClass.CreditCategory))
            {
                if (categoryMoves.Any(x => x.Reservation.LessonAccount is { } source && source.PeriodId != member.LessonPeriodId))
                    return "Eski paketin dersi yeni paket dönemine taşınamaz.";
                var account = allocation.Accounts.Single(x => x.Category == categoryMoves.Key);
                if (account.MonthlyLimit is not { } limit) continue;
                var existing = await db.ClassReservations.AsNoTracking().Where(x => x.MemberProfileId == member.Id &&
                    x.Status == ClassReservationStatus.Reserved && x.ClassSession.Status != ClassSessionStatus.Cancelled &&
                    !excludedSessionIds.Contains(x.ClassSessionId) &&
                    ((x.LessonAccount != null && x.LessonAccount.PeriodId == account.PeriodId && x.LessonAccount.Category == account.Category) ||
                     (x.LessonAccountId == null && x.ClassSession.GroupClass.CreditCategory == account.Category)) &&
                    (!member.MembershipStartsAtUtc.HasValue || x.ClassSession.StartsAtUtc >= member.MembershipStartsAtUtc) &&
                    (!member.MembershipEndsAtUtc.HasValue || x.ClassSession.StartsAtUtc < member.MembershipEndsAtUtc))
                    .Select(x => x.ClassSession.StartsAtUtc).ToListAsync();
                foreach (var month in categoryMoves.GroupBy(x => Month(x.Start)))
                    if (existing.Count(x => Month(x) == month.Key) + month.Count() > limit)
                        return $"{month.Key.Month:00}.{month.Key.Year} ayında bu kategoride en fazla {limit} ders planlanabilir. Hiçbir ders değiştirilmedi.";
            }
        }
        return null;
    }

    private static (int Year, int Month) Month(DateTime date)
    { var local = ClubTime.ToLocal(date); return (local.Year, local.Month); }
}
