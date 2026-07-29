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

    [Range(0, 1000000)]
    [Display(Name = "Paket fiyatı")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Range(1, 100)]
    [Display(Name = "Sıralama")]
    public int DisplayOrder { get; set; }
}
