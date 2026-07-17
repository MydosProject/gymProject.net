using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class MembershipPackageFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Paket kodu")]
    public MembershipPackageCode Code { get; set; }

    [Required]
    [StringLength(40)]
    [Display(Name = "Paket adı")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(160)]
    [Display(Name = "Hedef kitle")]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [StringLength(600)]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 14)]
    [Display(Name = "Haftalık ders hakkı")]
    public int? WeeklyClassLimit { get; set; }

    [Display(Name = "Ölçüm")]
    public bool IncludesMeasurement { get; set; }

    [Display(Name = "Vücut analizi")]
    public bool IncludesBodyAnalysis { get; set; }

    [Display(Name = "Beslenme desteği")]
    public bool IncludesNutritionSupport { get; set; }

    [Display(Name = "Detaylı takip")]
    public bool IncludesDetailedTracking { get; set; }

    [Display(Name = "Aylık analiz")]
    public bool IncludesMonthlyAnalysis { get; set; }

    [Display(Name = "Öncelikli rezervasyon")]
    public bool IncludesPriorityReservation { get; set; }

    [Display(Name = "Personal Training desteği")]
    public bool IncludesPersonalTrainingSupport { get; set; }

    [Display(Name = "Kitchen avantajları")]
    public bool IncludesKitchenBenefits { get; set; }

    [Display(Name = "Özel etkinlik")]
    public bool IncludesPrivateEvents { get; set; }

    [Display(Name = "Community üyeliği")]
    public bool IncludesCommunityMembership { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Range(1, 100)]
    [Display(Name = "Sıralama")]
    public int DisplayOrder { get; set; }
}
