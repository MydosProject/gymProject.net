namespace NO23.Web.ViewModels.Blog;

public class BlogPostDetailViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string? Tags { get; init; }

    public string? CoverImageUrl { get; init; }

    public DateTime? PublishedAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
