using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum MembershipStatus
{
    [Display(Name = "Aktif")]
    Active = 1,

    [Display(Name = "Dönem sonunda iptal edilecek")]
    CancellationScheduled = 2,

    [Display(Name = "İptal edildi")]
    Cancelled = 3,

    [Display(Name = "Sona erdi")]
    Expired = 4,

    [Display(Name = "Ödeme başarısız")]
    PaymentFailed = 5
}
