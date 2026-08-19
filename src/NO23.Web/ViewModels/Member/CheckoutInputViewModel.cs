using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Member;

public class CheckoutInputViewModel
{
    [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
    [StringLength(160, ErrorMessage = "Ad Soyad en fazla 160 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon alanı zorunludur.")]
    [StringLength(40, ErrorMessage = "Telefon en fazla 40 karakter olabilir.")]
    [RegularExpression(
        @"^05\d{2}\s?\d{3}\s?\d{2}\s?\d{2}$",
        ErrorMessage = "Telefon numarasını 05xx xxx xx xx formatında girmelisin.")]
    [Display(Name = "Telefon")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adres alanı zorunludur.")]
    [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
    [Display(Name = "Adres")]
    public string AddressLine { get; set; } = string.Empty;

    [Required(ErrorMessage = "İlçe alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "İlçe en fazla 100 karakter olabilir.")]
    [Display(Name = "İlçe")]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şehir alanı zorunludur.")]
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
}
