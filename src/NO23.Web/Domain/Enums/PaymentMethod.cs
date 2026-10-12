using System.ComponentModel.DataAnnotations;

namespace NO23.Web.Domain.Enums;

public enum PaymentMethod
{
    [Display(Name = "Kredi kartı")]
    CreditCard = 1,
    [Display(Name = "Nakit")]
    Cash = 2,
    [Display(Name = "Havale / EFT")]
    BankTransfer = 3
}
