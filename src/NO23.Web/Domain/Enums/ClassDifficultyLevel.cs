using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum ClassDifficultyLevel
{
    [Display(Name = "Başlangıç")]
    Beginner = 1,

    [Display(Name = "Orta")]
    Intermediate = 2,

    [Display(Name = "İleri")]
    Advanced = 3,

    [Display(Name = "Tüm seviyeler")]
    AllLevels = 4
}
