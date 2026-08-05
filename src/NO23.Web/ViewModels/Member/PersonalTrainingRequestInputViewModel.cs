using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Member;

public class PersonalTrainingRequestInputViewModel
{
    [Required]
    [Display(Name = "Eğitmen")]
    public int TrainerId { get; set; }

    [Required]
    [Display(Name = "Tercih edilen gün")]
    public DateOnly PreferredDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Required]
    [StringLength(40)]
    [Display(Name = "Saat aralığı")]
    public string PreferredTimeWindow { get; set; } = "09:00 - 12:00";

    [StringLength(1200)]
    [Display(Name = "Hedefin ve notun")]
    public string? GoalNote { get; set; }
}
