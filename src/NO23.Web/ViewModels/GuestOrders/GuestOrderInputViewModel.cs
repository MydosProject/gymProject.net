using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Web.ViewModels.GuestOrders;

public class GuestOrderInputViewModel : IValidatableObject
{
    [Range(1, 99, ErrorMessage = "Adet 1 ile 99 arasında olmalıdır.")]
    [Display(Name = "Adet")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Beden")]
    public int? ShopProductVariantId { get; set; }

    public List<int> RemovedKitchenIngredientIds { get; set; } = [];

    public List<int> AddedKitchenIngredientIds { get; set; } = [];

    [EnumDataType(typeof(OrderDeliveryMethod), ErrorMessage = "Geçerli bir teslimat yöntemi seçmelisin.")]
    [Display(Name = "Teslimat yöntemi")]
    public OrderDeliveryMethod DeliveryMethod { get; set; } =
        OrderDeliveryMethod.AddressDelivery;

    [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
    [StringLength(160, ErrorMessage = "Ad Soyad en fazla 160 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta alanı zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girmelisin.")]
    [StringLength(256, ErrorMessage = "E-posta en fazla 256 karakter olabilir.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon alanı zorunludur.")]
    [StringLength(40, ErrorMessage = "Telefon en fazla 40 karakter olabilir.")]
    [Display(Name = "Telefon")]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
    [Display(Name = "Adres")]
    public string AddressLine { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "İlçe en fazla 100 karakter olabilir.")]
    [Display(Name = "İlçe")]
    public string District { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Şehir en fazla 100 karakter olabilir.")]
    [Display(Name = "Şehir")]
    public string City { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Posta Kodu en fazla 20 karakter olabilir.")]
    [Display(Name = "Posta Kodu")]
    public string? PostalCode { get; set; }

    [Required(ErrorMessage = "Teslimat Tarihi alanı zorunludur.")]
    [Display(Name = "Teslimat Tarihi")]
    public DateOnly DeliveryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Required(ErrorMessage = "Teslimat Saati alanı zorunludur.")]
    [StringLength(40, ErrorMessage = "Teslimat Saati en fazla 40 karakter olabilir.")]
    [Display(Name = "Teslimat Saati")]
    public string DeliveryTimeSlot { get; set; } = "10:00-13:00";

    [StringLength(500, ErrorMessage = "Sipariş Notu en fazla 500 karakter olabilir.")]
    [Display(Name = "Sipariş Notu")]
    public string? Notes { get; set; }

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
