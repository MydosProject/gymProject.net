using NO23.Web.Domain.Enums;

namespace NO23.Web.ViewModels.Admin;

public class OrderListItemViewModel
{
    public int Id { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public string MemberName { get; init; } = string.Empty;

    public string? GuestEmail { get; init; }

    public string Type { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public OrderStatus RawStatus { get; init; }

    public string PaymentStatus { get; init; } = string.Empty;

    public PaymentStatus RawPaymentStatus { get; init; }

    public IReadOnlyList<OrderStatus> AvailableOrderStatuses { get; init; } = [];

    public IReadOnlyList<PaymentStatus> AvailablePaymentStatuses { get; init; } = [];

    public DateOnly DeliveryDate { get; init; }

    public string DeliveryTimeSlot { get; init; } = string.Empty;

    public decimal Total { get; init; }

    public int ItemCount { get; init; }
}
