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
            if (session.CreditConsumed && !unlimited) member.RemainingClassCredits++;
            session.CreditConsumed = false;
        }
        else if (!session.CreditConsumed)
        {
            if (!unlimited) member.RemainingClassCredits--;
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
