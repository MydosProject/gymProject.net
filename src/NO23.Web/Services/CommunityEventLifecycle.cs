using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public static class CommunityEventLifecycle
{
    public static CommunityEventStatus GetEffectiveStatus(
        CommunityEventStatus storedStatus,
        DateTime startsAtUtc,
        DateTime? endsAtUtc,
        DateTime nowUtc)
    {
        if (storedStatus == CommunityEventStatus.Cancelled)
        {
            return CommunityEventStatus.Cancelled;
        }

        var effectiveEndUtc = endsAtUtc ?? startsAtUtc;

        return effectiveEndUtc <= nowUtc
            ? CommunityEventStatus.Completed
            : CommunityEventStatus.Scheduled;
    }

    public static CommunityEventStatus NormalizeStoredStatus(
        CommunityEventStatus requestedStatus,
        DateTime startsAtUtc,
        DateTime? endsAtUtc,
        DateTime nowUtc)
    {
        return requestedStatus == CommunityEventStatus.Cancelled
            ? CommunityEventStatus.Cancelled
            : GetEffectiveStatus(requestedStatus, startsAtUtc, endsAtUtc, nowUtc);
    }

    public static bool IsPubliclyOpen(CommunityEventStatus effectiveStatus)
    {
        return effectiveStatus == CommunityEventStatus.Scheduled;
    }

    public static bool IsReservationOpen(
        CommunityEventStatus storedStatus,
        DateTime startsAtUtc,
        DateTime? endsAtUtc,
        DateTime nowUtc)
    {
        return startsAtUtc > nowUtc &&
               GetEffectiveStatus(
                   storedStatus,
                   startsAtUtc,
                   endsAtUtc,
                   nowUtc) == CommunityEventStatus.Scheduled;
    }
}
