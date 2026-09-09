namespace NO23.Web.Services;

public static class ClubTime
{
    public static DateTime Now => DateTime.UtcNow.AddHours(3);
    public static DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local.AddHours(-3), DateTimeKind.Utc);
    public static DateTime ToLocal(DateTime utc) => DateTime.SpecifyKind(utc.AddHours(3), DateTimeKind.Unspecified);
    public static DateTime Monday(DateTime date) => date.Date.AddDays(-((int)date.DayOfWeek + 6) % 7);
}
