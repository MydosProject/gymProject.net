using System.ComponentModel.DataAnnotations;
using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class ServicePackageListItemViewModel
{
    public int Id { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public int VariantCount { get; init; }
}

public class ServicePackageFormViewModel
{
    public int Id { get; set; }
    [Display(Name = "Kategori")] public ServicePackageCategory Category { get; set; }
    [Required, StringLength(120), Display(Name = "URL kodu")] public string Slug { get; set; } = string.Empty;
    [Required, StringLength(100), Display(Name = "Paket adı")] public string Name { get; set; } = string.Empty;
    [Required, StringLength(180), Display(Name = "Alt başlık")] public string Subtitle { get; set; } = string.Empty;
    [Required, StringLength(700), Display(Name = "Açıklama")] public string Description { get; set; } = string.Empty;
    [Display(Name = "Bağlı üyelik paketi")] public int? MembershipPackageId { get; set; }
    [Display(Name = "Öne çıkan paket")] public bool IsFeatured { get; set; }
    [Display(Name = "Aktif")] public bool IsActive { get; set; } = true;
    [Range(1, 1000), Display(Name = "Sıralama")] public int DisplayOrder { get; set; } = 10;
    [Display(Name = "Özellikler")] public string FeaturesText { get; set; } = string.Empty;
    public IReadOnlyList<MembershipPackageSelectOptionViewModel> MembershipPackages { get; set; } = [];
    public IReadOnlyList<ServicePackageVariantListItemViewModel> Variants { get; set; } = [];
}

public class ServicePackageVariantFormViewModel
{
    public int Id { get; set; }
    public int ServicePackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    [Required, StringLength(100), Display(Name = "Varyant adı")] public string Name { get; set; } = string.Empty;
    [Display(Name = "Ödeme tipi")] public ServicePackageBillingType BillingType { get; set; }
    [Range(1, 60), Display(Name = "Süre (ay)")] public int? DurationMonths { get; set; }
    [Range(1, 365), Display(Name = "Süre (gün)")] public int? DurationDays { get; set; }
    [Range(0, 10000000), Display(Name = "Aylık fiyat")] public decimal? MonthlyPrice { get; set; }
    [Range(0, 10000000), Display(Name = "Toplam fiyat")] public decimal TotalPrice { get; set; }
    [Range(0, 500), Display(Name = "PT seansı")] public int PersonalTrainingSessionCount { get; set; }
    [Range(0, 500), Display(Name = "Reformer hakkı")] public int ReformerClassCreditCount { get; set; }
    [Range(0, 500), Display(Name = "Performance hakkı")] public int PerformanceClassCreditCount { get; set; }
    [Range(0, 500), Display(Name = "Genel grup dersi hakkı")] public int GroupClassCreditCount { get; set; }
    [Range(0, 500), Display(Name = "Kids Club ders hakkı")] public int KidsClassCreditCount { get; set; }
    [Display(Name = "Salon erişimi")] public bool IncludesGymAccess { get; set; }
    [Display(Name = "Önerilen varyant")] public bool IsRecommended { get; set; }
    [Display(Name = "Aktif")] public bool IsActive { get; set; } = true;
    [Range(1, 1000), Display(Name = "Sıralama")] public int DisplayOrder { get; set; } = 10;
}

public class ServicePackageVariantListItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Price { get; init; } = string.Empty;
    public string Rights { get; init; } = string.Empty;
    public bool IsRecommended { get; init; }
    public bool IsActive { get; init; }
}
