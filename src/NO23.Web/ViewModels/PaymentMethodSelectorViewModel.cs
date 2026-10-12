using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels;

public sealed class PaymentMethodSelectorViewModel
{
    public string InputName { get; init; } = "PaymentMethod";
    public PaymentMethod SelectedMethod { get; init; } = PaymentMethod.CreditCard;
    public bool IsCardPaymentAvailable { get; init; }
}
