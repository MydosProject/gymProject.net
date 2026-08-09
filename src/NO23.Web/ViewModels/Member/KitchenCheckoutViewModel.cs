using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Member;

public class KitchenCheckoutViewModel
{
    public int KitchenSubscriptionId { get; set; }

    public string PackageName { get; set; } = string.Empty;

    public int PackageDays { get; set; }

    public decimal PackagePrice { get; set; }

    [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
    [StringLength(160)]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon alanı zorunludur.")]
    [StringLength(40)]
    [Display(Name = "Telefon")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adres alanı zorunludur.")]
    [StringLength(500)]
    [Display(Name = "Adres")]
    public string AddressLine { get; set; } = string.Empty;

    [Required(ErrorMessage = "İlçe alanı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "İlçe")]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şehir alanı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Şehir")]
    public string City { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Posta Kodu")]
    public string? PostalCode { get; set; }
}