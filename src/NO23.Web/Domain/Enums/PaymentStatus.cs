using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum PaymentStatus
{
    [Display(Name = "Ödeme bekleniyor")]
    Pending = 1,

    [Display(Name = "Ödendi")]
    Paid = 2,

    [Display(Name = "Başarısız")]
    Failed = 3,

    [Display(Name = "İade edildi")]
    Refunded = 4
}
