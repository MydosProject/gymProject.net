using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public static class ClassSessionLifecycle
{
    public static ClassSessionStatus GetEffectiveStatus(
        ClassSessionStatus storedStatus,
        DateTime startsAtUtc,
        DateTime nowUtc)
    {
        if (storedStatus is ClassSessionStatus.Cancelled or ClassSessionStatus.Completed)
        {
            return storedStatus;
        }

        return startsAtUtc <= nowUtc
            ? ClassSessionStatus.Completed
            : ClassSessionStatus.Scheduled;
    }

    public static bool IsReservationOpen(
        ClassSessionStatus storedStatus,
        DateTime startsAtUtc,
        DateTime nowUtc,
        bool isGroupClassActive)
    {
        return isGroupClassActive &&
               storedStatus == ClassSessionStatus.Scheduled &&
               GetEffectiveStatus(storedStatus, startsAtUtc, nowUtc) == ClassSessionStatus.Scheduled;
    }
}
