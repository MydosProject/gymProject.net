namespace NO23.Web.ViewModels.Admin;

public class CommunityChallengeDetailsViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateOnly StartsOn { get; init; }

    public DateOnly EndsOn { get; init; }

    public string Goal { get; init; } = string.Empty;

    public int TargetDailyCalories { get; init; }

    public decimal CalorieTolerancePercent { get; init; }

    public int MinDailyCalories { get; init; }

    public int MaxDailyCalories { get; init; }

    public int RequiredCompletionPercent { get; init; }

    public IReadOnlyList<CommunityChallengeParticipantViewModel> Participants { get; init; } = [];

    public IReadOnlyList<CommunityChallengeLogViewModel> RecentLogs { get; init; } = [];
}

public class CommunityChallengeParticipantViewModel
{
    public string MemberName { get; init; } = string.Empty;

    public string MemberEmail { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime JoinedAtUtc { get; init; }

    public int LoggedDays { get; init; }

    public int CompliantDays { get; init; }

    public int TotalDays { get; init; }

    public decimal ProgressPercent { get; init; }
}

public class CommunityChallengeLogViewModel
{
    public string MemberName { get; init; } = string.Empty;

    public DateOnly EntryDate { get; init; }

    public int CaloriesConsumed { get; init; }

    public int MinCalories { get; init; }

    public int MaxCalories { get; init; }

    public bool IsCompliant { get; init; }
}
