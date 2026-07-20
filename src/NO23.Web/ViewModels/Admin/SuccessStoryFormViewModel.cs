using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class SuccessStoryFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(140)]
    [Display(Name = "Member name")]
    public string MemberName { get; set; } = string.Empty;

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
    [StringLength(8000)]
    [Display(Name = "Story")]
    public string Story { get; set; } = string.Empty;

    [StringLength(160)]
    [Display(Name = "Achievement metric")]
    public string? AchievementMetric { get; set; }

    [StringLength(500)]
    [Display(Name = "Before image URL")]
    public string? BeforeImageUrl { get; set; }

    [StringLength(500)]
    [Display(Name = "After image URL")]
    public string? AfterImageUrl { get; set; }

    [StringLength(500)]
    [Display(Name = "Video URL")]
    public string? VideoUrl { get; set; }

    [Display(Name = "Status")]
    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    [Display(Name = "Published at")]
    public DateTime? PublishedAtUtc { get; set; }
}
