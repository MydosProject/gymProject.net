using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class CommunityChallengeFormViewModel
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
    [StringLength(4000)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "Goal")]
    public string Goal { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Reward")]
    public string? Reward { get; set; }

    [Display(Name = "Starts on")]
    public DateOnly StartsOn { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

    [Display(Name = "Ends on")]
    public DateOnly EndsOn { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(28));

    [Display(Name = "Status")]
    public CommunityChallengeStatus Status { get; set; } = CommunityChallengeStatus.Upcoming;

    [StringLength(500)]
    [Display(Name = "Image URL")]
    public string? ImageUrl { get; set; }

    [Range(1, 100)]
    [Display(Name = "Display order")]
    public int DisplayOrder { get; set; } = 10;
}
