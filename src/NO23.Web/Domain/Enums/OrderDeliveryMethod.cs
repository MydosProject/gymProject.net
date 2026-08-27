using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum OrderDeliveryMethod
{
    [Display(Name = "Adrese teslim")]
    AddressDelivery = 1,

    [Display(Name = "Salondan teslim")]
    ClubPickup = 2
}
