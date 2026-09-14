using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public static class ClassSessionLifecycle
{
    public static ClassSessionStatus GetEffectiveStatus(
        ClassSessionStatus storedStatus,
        DateTime startsAtUtc,
        DateTime nowUtc,
        int durationMinutes = 60)
    {
        if (storedStatus is ClassSessionStatus.Cancelled or ClassSessionStatus.Completed)
        {
            return storedStatus;
        }

        return startsAtUtc.AddMinutes(Math.Max(1, durationMinutes)) <= nowUtc
            ? ClassSessionStatus.Completed
            : ClassSessionStatus.Scheduled;
    }

    public static bool IsReservationOpen(
        ClassSessionStatus storedStatus,
        DateTime startsAtUtc,
        DateTime nowUtc,
        bool isGroupClassActive,
        int durationMinutes = 60)
    {
        return isGroupClassActive &&
               storedStatus == ClassSessionStatus.Scheduled &&
               startsAtUtc > nowUtc &&
               GetEffectiveStatus(storedStatus, startsAtUtc, nowUtc, durationMinutes) == ClassSessionStatus.Scheduled;
    }
}
