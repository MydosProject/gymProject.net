using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum CartItemType
{
    [Display(Name = "NO23 Kitchen")]
    KitchenMenuItem = 1,

    [Display(Name = "NO23 Shop")]
    ShopProduct = 2,

    [Display(Name = "NO23 Kitchen Paketi")]
    KitchenSubscriptionPackage = 3
}
