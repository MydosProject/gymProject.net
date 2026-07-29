namespace NO23.Web.ViewModels.Member;

public class MemberCommunityIndexViewModel
{
    public bool HasCommunityMembership { get; init; }

    public IReadOnlyList<MemberCommunityChallengeCardViewModel> Challenges { get; init; } = [];
}

public class MemberCommunityChallengeCardViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Goal { get; init; } = string.Empty;

    public string? Reward { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateOnly StartsOn { get; init; }

    public DateOnly EndsOn { get; init; }

    public int TargetDailyCalories { get; init; }

    public int MinDailyCalories { get; init; }

    public int MaxDailyCalories { get; init; }

    public int RequiredCompletionPercent { get; init; }

    public int ParticipantCount { get; init; }

    public bool IsJoined { get; init; }
}
