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

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Parolalar eşleşmiyor.")]
    [Display(Name = "Parola tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;
}