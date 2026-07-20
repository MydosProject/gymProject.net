using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Member;

public class CheckoutInputViewModel
{
    [Required]
    [StringLength(160)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    [Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "Address")]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "District")]
    public string District { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Postal code")]
    public string? PostalCode { get; set; }

    [Required]
    [Display(Name = "Delivery date")]
    public DateOnly DeliveryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Required]
    [StringLength(40)]
    [Display(Name = "Delivery time")]
    public string DeliveryTimeSlot { get; set; } = "10:00-13:00";

    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}
