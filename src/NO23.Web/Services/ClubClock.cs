namespace NO23.Web.Services;

public interface IClubClock
{
    DateTime Now { get; }
    DateTime Today { get; }
    DateOnly TodayDate { get; }
}

public sealed class ClubClock(TimeProvider timeProvider) : IClubClock
{
    public DateTime Now => ClubTime.ToLocal(timeProvider.GetUtcNow().UtcDateTime);
    public DateTime Today => Now.Date;
    public DateOnly TodayDate => DateOnly.FromDateTime(Now);
}
