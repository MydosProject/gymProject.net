namespace NO23.Web.ViewModels.Admin;

public class SuccessStoryListItemViewModel
{
    public int Id { get; init; }

    public string MemberName { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string? AchievementMetric { get; init; }

    public DateTime? PublishedAtUtc { get; init; }
}
