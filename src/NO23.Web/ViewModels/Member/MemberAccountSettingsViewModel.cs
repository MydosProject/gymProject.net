using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Member;

public class MemberAccountSettingsViewModel
{
    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public bool EmailConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public bool HasPassword { get; set; }

    public bool HasMembershipPackage { get; set; }

    public string MembershipPackageName { get; set; } = string.Empty;

    public DateTime? MembershipStartsAtUtc { get; set; }

    public DateTime? MembershipEndsAtUtc { get; set; }

    public string MembershipStatusDisplayName { get; set; } = string.Empty;

    public DateTime? MembershipCancellationRequestedAtUtc { get; set; }

    public DateTime? MembershipCancellationEffectiveAtUtc { get; set; }

    public bool CanCancelMembership { get; set; }

    public bool HasPendingPackageChangeRequest { get; set; }

    public string? PendingPackageChangeRequestName { get; set; }

    public IReadOnlyList<MemberMembershipPackageOptionViewModel> MembershipPackageOptions { get; set; } = [];

    public ChangePasswordInputViewModel ChangePassword { get; set; } = new();
}

public class MemberMembershipPackageOptionViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ClassAccessSummary { get; init; } = string.Empty;

    public bool IsCurrentPackage { get; init; }
}

public class ChangePasswordInputViewModel
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
