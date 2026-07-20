namespace NO23.Web.ViewModels.Admin;

public class CommunityChallengeListItemViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateOnly StartsOn { get; init; }

    public DateOnly EndsOn { get; init; }

    public string Goal { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}
