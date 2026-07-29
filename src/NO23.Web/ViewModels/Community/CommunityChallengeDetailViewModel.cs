namespace NO23.Web.ViewModels.Community;

public class CommunityChallengeDetailViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Goal { get; init; } = string.Empty;

    public string? Reward { get; init; }

    public int TargetDailyCalories { get; init; }

    public decimal CalorieTolerancePercent { get; init; }

    public int MinDailyCalories { get; init; }

    public int MaxDailyCalories { get; init; }

    public int RequiredCompletionPercent { get; init; }

    public DateOnly StartsOn { get; init; }

    public DateOnly EndsOn { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? ImageUrl { get; init; }

    public bool IsJoined { get; init; }

    public bool CanJoin { get; init; }

    public string? JoinMessage { get; init; }

    public int? MyParticipationId { get; init; }

    public decimal MyProgressPercent { get; init; }

    public int MyCompliantDays { get; init; }

    public int MyLoggedDays { get; init; }

    public int TotalDays { get; init; }

    public IReadOnlyList<ChallengeLeaderboardItemViewModel> Leaderboard { get; init; } = [];
}
