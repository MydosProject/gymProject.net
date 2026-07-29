namespace NO23.Web.ViewModels.Community;

public class ChallengeLeaderboardItemViewModel
{
    public int Rank { get; init; }

    public string MemberName { get; init; } = string.Empty;

    public decimal ProgressPercent { get; init; }

    public int CompliantDays { get; init; }

    public int LoggedDays { get; init; }

    public int TotalDays { get; init; }

    public bool IsCompleted { get; init; }
}
