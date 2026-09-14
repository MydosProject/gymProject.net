using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class KitchenSubscriptionPackageFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Plan")]
    public KitchenSubscriptionPlan Plan { get; set; }

    [Required]
    [StringLength(80)]
    [Display(Name = "Paket adı")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(600)]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 365)]
    [Display(Name = "Gün sayısı")]
    public int Days { get; set; }

    [Range(typeof(decimal), "0.01", "1000000")]
    [Display(Name = "1 ana + 1 ara öğün")]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0.01", "1000000")]
    [Display(Name = "2 ana + 1 ara öğün")]
    public decimal TwoMainMealsPrice { get; set; }

    [Range(typeof(decimal), "0.01", "1000000")]
    [Display(Name = "3 ana + 1 ara öğün")]
    public decimal ThreeMainMealsPrice { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    [Display(Name = "Günlük kurye ücreti")]
    public decimal DailyDeliveryFee { get; set; } = 95;

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Range(1, 100)]
    [Display(Name = "Sıralama")]
    public int DisplayOrder { get; set; }
}
