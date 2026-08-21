using System.ComponentModel.DataAnnotations;

namespace NO23.Web.ViewModels.Admin;

public class MembershipPackageOptionFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue), Display(Name = "Üyelik paketi")]
    public int MembershipPackageId { get; set; }

    [Required, StringLength(100), Display(Name = "Seçenek adı")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(500), Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 365), Display(Name = "Süre (gün)")]
    public int DurationDays { get; set; } = 20;

    [Range(0, 100), Display(Name = "PT seans hakkı")]
    public int PersonalTrainingSessionCount { get; set; }

    [Range(0, 500), Display(Name = "Grup dersi hakkı")]
    public int GroupClassCreditCount { get; set; }

    [Display(Name = "Salon kullanımını içerir")]
    public bool IncludesGymAccess { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Range(1, 1000), Display(Name = "Sıralama")]
    public int DisplayOrder { get; set; } = 10;

    public IReadOnlyList<MembershipPackageSelectOptionViewModel> PackageOptions { get; set; } = [];
}

public class MembershipPackageSelectOptionViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
