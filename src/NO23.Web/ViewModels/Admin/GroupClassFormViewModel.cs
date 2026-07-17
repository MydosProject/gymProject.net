using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class GroupClassFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Eğitmen")]
    public int TrainerId { get; set; }

    [Required]
    [StringLength(120)]
    [Display(Name = "Ders adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(800)]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Range(15, 180)]
    [Display(Name = "Süre")]
    public int DurationMinutes { get; set; }

    [Display(Name = "Zorluk")]
    public ClassDifficultyLevel DifficultyLevel { get; set; }

    [Range(1, 2000)]
    [Display(Name = "Ortalama kalori")]
    public int AverageCaloriesBurned { get; set; }

    [Range(1, 100)]
    [Display(Name = "Kapasite")]
    public int Capacity { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<SelectListItem> TrainerOptions { get; set; } = [];
}
