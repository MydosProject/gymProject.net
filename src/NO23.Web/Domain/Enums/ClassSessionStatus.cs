using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum ClassSessionStatus
{
    [Display(Name = "Planlanmış")]
    Scheduled = 1,

    [Display(Name = "İptal edildi")]
    Cancelled = 2,

    [Display(Name = "Tamamlandı")]
    Completed = 3
}
