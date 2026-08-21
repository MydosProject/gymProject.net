using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum PersonalTrainingSessionStatus
{
    [Display(Name = "Planlandı")]
    Scheduled = 1,

    [Display(Name = "Tamamlandı")]
    Completed = 2,

    [Display(Name = "İptal edildi")]
    Cancelled = 3,

    [Display(Name = "Gelmedi")]
    NoShow = 4,

    [Display(Name = "Ertelendi")]
    Postponed = 5
}
