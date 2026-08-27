using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Plans;

public class PlanApplicationPageViewModel
{
    public string PackageName { get; init; } = string.Empty;

    public string PackageCategory { get; init; } = string.Empty;

    public string CategoryRoute { get; init; } = string.Empty;

    public string VariantName { get; init; } = string.Empty;

    public string VariantPrice { get; init; } = string.Empty;

    public string VariantRights { get; init; } = string.Empty;

    public PlanApplicationInputViewModel Input { get; init; } = new();
}

public class PlanApplicationInputViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir paket seçmelisin.")]
    public int ServicePackageId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir paket seçeneği seçmelisin.")]
    public int ServicePackageVariantId { get; set; }

    [Required(ErrorMessage = "Ad soyad alanı zorunludur.")]
    [StringLength(160)]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta alanı zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girmelisin.")]
    [StringLength(256)]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon alanı zorunludur.")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası girmelisin.")]
    [StringLength(40)]
    [Display(Name = "Telefon")]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Notun")]
    public string? Notes { get; set; }
}
