using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum OrderStatus
{
    [Display(Name = "Beklemede")]
    Pending = 1,

    [Display(Name = "Onaylandı")]
    Confirmed = 2,

    [Display(Name = "Hazırlanıyor")]
    Preparing = 3,

    [Display(Name = "Teslimata çıktı")]
    OutForDelivery = 4,

    [Display(Name = "Teslim edildi")]
    Delivered = 5,

    [Display(Name = "İptal edildi")]
    Cancelled = 6
}
