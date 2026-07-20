using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class BlogPostFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(180)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(180)]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "Summary")]
    public string Summary { get; set; } = string.Empty;

    [Required]
    [StringLength(12000)]
    [Display(Name = "Content")]
    public string Content { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Tags")]
    public string? Tags { get; set; }

    [StringLength(500)]
    [Display(Name = "Cover image URL")]
    public string? CoverImageUrl { get; set; }

    [Display(Name = "Status")]
    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    [Display(Name = "Published at")]
    public DateTime? PublishedAtUtc { get; set; }
}
