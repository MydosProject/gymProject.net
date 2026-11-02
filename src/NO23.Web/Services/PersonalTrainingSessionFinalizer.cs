using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

internal static class PersonalTrainingSessionFinalizer
{
    public static void Apply(PersonalTrainingSession session, PersonalTrainingSessionStatus status,
        string changedByUserId, string? note, DateTime now)
    {
        if (session.Status != PersonalTrainingSessionStatus.Scheduled ||
            status is not (PersonalTrainingSessionStatus.Completed or PersonalTrainingSessionStatus.Cancelled or PersonalTrainingSessionStatus.NoShow))
            throw new InvalidOperationException("Yalnızca planlanmış bir ders sonuçlandırılabilir.");

        var member = session.MemberProfile;
        var unlimited = MemberPackageEntitlement.HasUnlimitedClassAccess(member);
        if (status == PersonalTrainingSessionStatus.Cancelled)
        {
            if (session.CreditConsumed && session.LessonAccount is { } refundAccount)
                CategoryCreditService.Change(member, refundAccount, 1, changedByUserId, "Birebir iptal: aynı kategoriye hak iadesi.");
            else if (session.CreditConsumed && member.LessonPeriodId is null && !unlimited) member.RemainingClassCredits++;
            session.CreditConsumed = false;
        }
        else if (!session.CreditConsumed)
        {
            if (session.LessonAccount is null && member.LessonPeriodId.HasValue)
                throw new InvalidOperationException("Birebir dersin kaynak hak hesabı doğrulanmalı.");
            if (session.LessonAccount is { } account)
                CategoryCreditService.Change(member, account, -1, changedByUserId, "Birebir tamamlandı / katılmadı.");
            else if (member.LessonPeriodId is null && !unlimited) member.RemainingClassCredits--;
            session.CreditConsumed = true;
        }

        member.UpdatedAtUtc = now;
        session.Status = status;
        session.UpdatedAtUtc = now;
        session.History.Add(new PersonalTrainingSessionHistory
        {
            PreviousStatus = PersonalTrainingSessionStatus.Scheduled,
            NewStatus = status,
            PreviousStartsAtUtc = session.StartsAtUtc,
            NewStartsAtUtc = session.StartsAtUtc,
            ChangedByUserId = changedByUserId,
            Note = note?.Trim(),
            ChangedAtUtc = now
        });
    }
}
