using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Member;

public class ChallengeCalorieLogInputViewModel
{
    [Required]
    public int ParticipationId { get; set; }

    [Required(ErrorMessage = "Tarih alanı zorunludur.")]
    [Display(Name = "Tarih")]
    public DateOnly EntryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Alınan kalori alanı zorunludur.")]
    [Range(1, 10000, ErrorMessage = "Alınan kalori 1 ile 10000 kcal arasında olmalıdır.")]
    [Display(Name = "Alınan kalori")]
    public int CaloriesConsumed { get; set; }
}
