using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum PersonalTrainingRequestStatus
{
    [Display(Name = "Beklemede")]
    Pending = 1,

    [Display(Name = "Planlandı")]
    Scheduled = 2,

    [Display(Name = "Reddedildi")]
    Rejected = 3,

    [Display(Name = "İptal edildi")]
    Cancelled = 4,

    [Display(Name = "Tamamlandı")]
    Completed = 5
}
