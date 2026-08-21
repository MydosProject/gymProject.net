using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Admin;

public class AdminAccountSettingsViewModel
{
    public string Email { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public bool HasPassword { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public AdminProfileInputViewModel Profile { get; set; } = new();

    public AdminChangePasswordInputViewModel ChangePassword { get; set; } = new();
}

public class AdminProfileInputViewModel
{
    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    [StringLength(80, ErrorMessage = "Ad en fazla 80 karakter olabilir.")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    [StringLength(80, ErrorMessage = "Soyad en fazla 80 karakter olabilir.")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Geçerli bir telefon numarası girmelisin.")]
    [StringLength(32, ErrorMessage = "Telefon numarası en fazla 32 karakter olabilir.")]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }
}

public class AdminChangePasswordInputViewModel
{
    [Required(ErrorMessage = "Mevcut şifreni girmelisin.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut şifre")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifreni girmelisin.")]
    [StringLength(
        100,
        ErrorMessage = "Yeni şifre en az {2}, en fazla {1} karakter olmalıdır.",
        MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni şifre")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifreni tekrar girmelisin.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni şifre tekrar")]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "Yeni şifre ve tekrar alanı eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
