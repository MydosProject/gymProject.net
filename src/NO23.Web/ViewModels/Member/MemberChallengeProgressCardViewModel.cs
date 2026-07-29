namespace NO23.Web.ViewModels.Member;

public class MemberChallengeProgressCardViewModel
{
    public int ParticipationId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateOnly StartsOn { get; init; }

    public DateOnly EndsOn { get; init; }

    public int TargetDailyCalories { get; init; }

    public int MinDailyCalories { get; init; }

    public int MaxDailyCalories { get; init; }

    public int RequiredCompletionPercent { get; init; }

    public int LoggedDays { get; init; }

    public int CompliantDays { get; init; }

    public int TotalDays { get; init; }

    public decimal ProgressPercent { get; init; }

    public bool IsCompleted { get; init; }

    public bool CanLogToday { get; init; }

    public DateOnly LogDate { get; init; }

    public int? TodayCaloriesConsumed { get; init; }

    public bool? TodayIsCompliant { get; init; }
}
