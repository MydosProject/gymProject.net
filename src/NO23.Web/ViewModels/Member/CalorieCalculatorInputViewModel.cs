using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Member;

public class CalorieCalculatorInputViewModel
{
    [Range(120, 230)]
    [Display(Name = "Boy")]
    public int HeightCm { get; set; } = 170;

    [Range(35, 250)]
    [Display(Name = "Kilo")]
    public decimal WeightKg { get; set; } = 70;

    [Range(13, 90)]
    [Display(Name = "Yaş")]
    public int Age { get; set; } = 28;

    [Display(Name = "Cinsiyet")]
    public Gender Gender { get; set; } = Gender.Female;

    [Display(Name = "Günlük aktivite")]
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.ModeratelyActive;

    [Display(Name = "Hedef")]
    public NutritionGoal Goal { get; set; } = NutritionGoal.HealthyLifestyle;
}
