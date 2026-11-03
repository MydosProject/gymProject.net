namespace NO23.Web.ViewModels.Classes;

public class ClassesIndexViewModel
{
    public IReadOnlyList<GroupClassPublicViewModel> GroupClasses { get; init; } = [];

    public IReadOnlyList<UpcomingClassSessionPublicViewModel> UpcomingSessions { get; init; } = [];

    public string ReservationTargetUrl { get; init; } = string.Empty;

    public DateTime WeekStart { get; init; }

    public DateOnly? PreviousWeek { get; init; }

    public DateOnly NextWeek { get; init; }

    public bool IsCurrentWeek { get; init; }

    public IReadOnlyList<ClassScheduleDayViewModel> WeekDays { get; init; } = [];
}

public class ClassScheduleDayViewModel
{
    public DateTime Date { get; init; }

    public string DayName { get; init; } = string.Empty;

    public IReadOnlyList<UpcomingClassSessionPublicViewModel> Sessions { get; init; } = [];
}
