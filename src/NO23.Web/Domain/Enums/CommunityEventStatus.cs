using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum CommunityEventStatus
{
    [Display(Name = "Planlanmış")]
    Scheduled = 1,

    [Display(Name = "Tamamlandı")]
    Completed = 2,

    [Display(Name = "İptal edildi")]
    Cancelled = 3
}
