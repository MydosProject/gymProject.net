using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class CommunityEventFormViewModel
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

    [Display(Name = "Type")]
    public CommunityEventType Type { get; set; }

    [Display(Name = "Status")]
    public CommunityEventStatus Status { get; set; } = CommunityEventStatus.Scheduled;

    [Display(Name = "Starts at")]
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow.Date.AddDays(7).AddHours(9);

    [Display(Name = "Ends at")]
    public DateTime? EndsAtUtc { get; set; }

    [Required]
    [StringLength(180)]
    [Display(Name = "Location")]
    public string Location { get; set; } = string.Empty;

    [Range(1, 10000)]
    [Display(Name = "Capacity")]
    public int? Capacity { get; set; }

    [Display(Name = "Members only")]
    public bool IsMembersOnly { get; set; } = true;

    [StringLength(500)]
    [Display(Name = "Image URL")]
    public string? ImageUrl { get; set; }

    [Range(1, 100)]
    [Display(Name = "Display order")]
    public int DisplayOrder { get; set; } = 10;
}
