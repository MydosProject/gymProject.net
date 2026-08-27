using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Web.ViewModels.Member;

public class KitchenCheckoutViewModel : IValidatableObject
{
    public int KitchenSubscriptionId { get; set; }

    public string PackageName { get; set; } = string.Empty;

    public int PackageDays { get; set; }

    public decimal PackagePrice { get; set; }

    public bool IsPaymentAvailable { get; set; }

    public string ClubPickupDisplayName { get; set; } = "NO23 Sports Club";

    [EnumDataType(typeof(OrderDeliveryMethod), ErrorMessage = "Geçerli bir teslimat yöntemi seçmelisin.")]
    [Display(Name = "Teslimat yöntemi")]
    public OrderDeliveryMethod DeliveryMethod { get; set; } =
        OrderDeliveryMethod.AddressDelivery;

    [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
    [StringLength(160)]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon alanı zorunludur.")]
    [StringLength(40)]
    [Display(Name = "Telefon")]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Adres")]
    public string AddressLine { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "İlçe")]
    public string District { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Şehir")]
    public string City { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Posta Kodu")]
    public string? PostalCode { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (DeliveryMethod != OrderDeliveryMethod.AddressDelivery)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(AddressLine))
        {
            yield return new ValidationResult(
                "Adres alanı zorunludur.",
                [nameof(AddressLine)]);
        }

        if (string.IsNullOrWhiteSpace(City))
        {
            yield return new ValidationResult(
                "Şehir alanı zorunludur.",
                [nameof(City)]);
        }

        if (string.IsNullOrWhiteSpace(District))
        {
            yield return new ValidationResult(
                "İlçe alanı zorunludur.",
                [nameof(District)]);
        }

        var locationCatalog = validationContext.GetService(
            typeof(TurkeyLocationCatalog)) as TurkeyLocationCatalog;

        if (locationCatalog is not null &&
            !string.IsNullOrWhiteSpace(City) &&
            !string.IsNullOrWhiteSpace(District) &&
            !locationCatalog.IsValid(City, District))
        {
            yield return new ValidationResult(
                "Seçilen il ve ilçe birbiriyle eşleşmiyor.",
                [nameof(City), nameof(District)]);
        }
    }
}
