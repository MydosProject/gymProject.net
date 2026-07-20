using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class CommunityEvent
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public CommunityEventType Type { get; set; }

    public CommunityEventStatus Status { get; set; } = CommunityEventStatus.Scheduled;

    public DateTime StartsAtUtc { get; set; }

    public DateTime? EndsAtUtc { get; set; }

    public string Location { get; set; } = string.Empty;

    public int? Capacity { get; set; }

    public bool IsMembersOnly { get; set; } = true;

    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
