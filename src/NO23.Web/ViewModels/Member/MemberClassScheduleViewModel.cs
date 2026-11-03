using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Member;

public sealed record MemberClassScheduleQuery(
    DateOnly? Date = null,
    string? View = null,
    LessonCategory? Category = null,
    int? ClassId = null);

public sealed record MemberScheduleClassOption(int Id, string Name, LessonCategory Category);

public class MemberClassScheduleViewModel
{
    public const string DayView = "day";
    public const string WeekView = "week";

    public string View { get; init; } = WeekView;

    public bool IsDaily => View == DayView;

    public DateTime SelectedDate { get; init; }

    public DateTime FirstDay { get; init; }

    public DateTime LastDay { get; init; }

    public DateTime RangeStart { get; init; }

    public DateTime RangeEnd { get; init; }

    public DateOnly? PreviousDate { get; init; }

    public DateOnly? NextDate { get; init; }

    public LessonCategory? Category { get; init; }

    public int? ClassId { get; init; }

    public IReadOnlyList<MemberScheduleClassOption> Classes { get; init; } = [];

    public IReadOnlyList<LessonCategory> Categories =>
        Classes.Select(option => option.Category).Distinct().Order().ToList();

    public IReadOnlyList<MemberScheduleDayViewModel> Days { get; init; } = [];

    public IReadOnlyList<AvailableClassSessionViewModel> Sessions { get; init; } = [];
}

public class MemberScheduleDayViewModel
{
    public DateTime Date { get; init; }

    public string DayName { get; init; } = string.Empty;

    public IReadOnlyList<AvailableClassSessionViewModel> Sessions { get; init; } = [];
}
