namespace NO23.Web.ViewModels.Community;

public class CommunityIndexViewModel
{
    public IReadOnlyList<CommunityEventCardViewModel> Events { get; init; } = [];

    public IReadOnlyList<CommunityChallengeCardViewModel> Challenges { get; init; } = [];
}
