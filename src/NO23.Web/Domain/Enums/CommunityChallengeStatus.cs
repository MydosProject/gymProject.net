using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum CommunityChallengeStatus
{
    [Display(Name = "Yakında")]
    Upcoming = 1,

    [Display(Name = "Devam Ediyor")]
    Active = 2,

    [Display(Name = "Tamamlandı")]
    Completed = 3,

    [Display(Name = "İptal Edildi")]
    Cancelled = 4
}
