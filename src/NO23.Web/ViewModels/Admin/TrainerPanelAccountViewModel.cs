using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Admin;

public class TrainerPanelAccountViewModel
{
    public int TrainerId { get; set; }

    public string TrainerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

}
