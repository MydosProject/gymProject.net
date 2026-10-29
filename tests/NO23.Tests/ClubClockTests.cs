using NO23.Web.Services;

namespace NO23.Tests;

internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

public class ClubClockTests
{
    [Theory]
    [InlineData("2026-10-29T20:59:59Z", "2026-10-29", 23)]
    [InlineData("2026-10-29T21:00:00Z", "2026-10-30", 0)]
    [InlineData("2026-11-01T21:30:00Z", "2026-11-02", 0)]
    [InlineData("2026-12-31T21:30:00Z", "2027-01-01", 0)]
    public void BusinessDateUsesIstanbulAtDayWeekAndYearBoundaries(string utc, string date, int hour)
    {
        var clock = new ClubClock(new FixedTimeProvider(DateTimeOffset.Parse(utc)));
        Assert.Equal(DateOnly.Parse(date), clock.TodayDate);
        Assert.Equal(hour, clock.Now.Hour);
        Assert.Equal(clock.TodayDate.ToDateTime(TimeOnly.MinValue), clock.Today);
    }

    [Fact]
    public void CalendarConversionPreservesInstantAndLocalWallClock()
    {
        var local = new DateTime(2026, 11, 2, 0, 30, 0);
        var utc = ClubTime.ToUtc(local);
        Assert.Equal(new DateTime(2026, 11, 1, 21, 30, 0, DateTimeKind.Utc), utc);
        Assert.Equal(local, ClubTime.ToLocal(utc));
        Assert.Equal(DateTimeKind.Unspecified, ClubTime.ToLocal(utc).Kind);
    }
}
