namespace NO23.Web.ViewModels.GuestOrders;

public class GuestOrderConfirmationViewModel
{
    public string OrderNumber { get; init; } = string.Empty;

    public string ProductName { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal Total { get; init; }

    public DateOnly DeliveryDate { get; init; }

    public string DeliveryTimeSlot { get; init; } = string.Empty;

    public string PaymentStatusText { get; init; } = "Ödeme Bekliyor";
}
