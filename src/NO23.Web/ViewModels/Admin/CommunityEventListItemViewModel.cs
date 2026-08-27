namespace NO23.Web.ViewModels.Admin;

public class CommunityEventListItemViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime StartsAtUtc { get; init; }

    public string Location { get; init; } = string.Empty;

    public int? Capacity { get; init; }

    public int ReservedCount { get; init; }

    public int DisplayOrder { get; init; }
}
