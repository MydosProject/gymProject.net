namespace NO23.Web.ViewModels.Admin;

public class BlogPostListItemViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime? PublishedAtUtc { get; init; }
}
