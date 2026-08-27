namespace NO23.Web.ViewModels.Community;

public class CommunityEventCardViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public DateTime StartsAtUtc { get; init; }

    public string Location { get; init; } = string.Empty;

    public int? Capacity { get; init; }

    public int ReservedCount { get; init; }

    public bool IsReserved { get; init; }

    public int? RemainingCapacity => Capacity.HasValue
        ? Math.Max(0, Capacity.Value - ReservedCount)
        : null;

    public bool IsMembersOnly { get; init; }

    public string? ImageUrl { get; init; }
}
