using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public static class CommunityChallengeLifecycle
{
    public static CommunityChallengeStatus GetEffectiveStatus(
        CommunityChallengeStatus storedStatus,
        DateOnly startsOn,
        DateOnly endsOn,
        DateOnly today)
    {
        if (storedStatus == CommunityChallengeStatus.Cancelled)
        {
            return CommunityChallengeStatus.Cancelled;
        }

        if (today < startsOn)
        {
            return CommunityChallengeStatus.Upcoming;
        }

        return today > endsOn
            ? CommunityChallengeStatus.Completed
            : CommunityChallengeStatus.Active;
    }

    public static CommunityChallengeStatus NormalizeStoredStatus(
        CommunityChallengeStatus requestedStatus,
        DateOnly startsOn,
        DateOnly endsOn,
        DateOnly today)
    {
        return requestedStatus == CommunityChallengeStatus.Cancelled
            ? CommunityChallengeStatus.Cancelled
            : GetEffectiveStatus(requestedStatus, startsOn, endsOn, today);
    }

    public static bool IsJoinOpen(CommunityChallengeStatus effectiveStatus)
    {
        return effectiveStatus is CommunityChallengeStatus.Upcoming or CommunityChallengeStatus.Active;
    }

    public static bool CanLogCalories(CommunityChallengeStatus effectiveStatus)
    {
        return effectiveStatus == CommunityChallengeStatus.Active;
    }
}
