namespace NO23.Web.Services;

public static class ClubTime
{
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    public static DateTime Now => ToLocal(TimeProvider.System.GetUtcNow().UtcDateTime);
    public static DateTime Today => Now.Date;
    public static DateTime ToUtc(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Istanbul);
    public static DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Istanbul);
    public static DateTime Monday(DateTime date) => date.Date.AddDays(-((int)date.DayOfWeek + 6) % 7);
}
