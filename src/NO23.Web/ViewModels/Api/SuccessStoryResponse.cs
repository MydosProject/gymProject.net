namespace NO23.Web.ViewModels.Api;

public class SuccessStoryResponse
{
    public int Id { get; init; }

    public string MemberName { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string? AchievementMetric { get; init; }

    public string? BeforeImageUrl { get; init; }

    public string? AfterImageUrl { get; init; }

    public string? VideoUrl { get; init; }

    public DateTime? PublishedAtUtc { get; init; }
}
