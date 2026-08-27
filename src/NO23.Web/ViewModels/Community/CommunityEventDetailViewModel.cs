namespace NO23.Web.ViewModels.Community;

public class CommunityEventDetailViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public DateTime StartsAtUtc { get; init; }

    public DateTime? EndsAtUtc { get; init; }

    public string Location { get; init; } = string.Empty;

    public int? Capacity { get; init; }

    public int ReservedCount { get; init; }

    public int? RemainingCapacity { get; init; }

    public bool IsReserved { get; init; }

    public bool CanReserve { get; init; }

    public string? ReservationMessage { get; init; }

    public bool IsMembersOnly { get; init; }

    public string? ImageUrl { get; init; }
}
