using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum OrderType
{
    [Display(Name = "Tek seferlik")]
    OneTime = 1,

    [Display(Name = "Kitchen aboneliği")]
    KitchenSubscription = 2
}
