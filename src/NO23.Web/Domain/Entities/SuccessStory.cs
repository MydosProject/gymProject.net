using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class SuccessStory
{
    public int Id { get; set; }

    public string MemberName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Story { get; set; } = string.Empty;

    public string? AchievementMetric { get; set; }

    public string? BeforeImageUrl { get; set; }

    public string? AfterImageUrl { get; set; }

    public string? VideoUrl { get; set; }

    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    public DateTime? PublishedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
