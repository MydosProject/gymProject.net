using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class Order
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public int? MemberProfileId { get; set; }

    public MemberProfile? MemberProfile { get; set; }

    public string? GuestEmail { get; set; }

    public OrderType Type { get; set; } = OrderType.OneTime;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public OrderDeliveryMethod DeliveryMethod { get; set; } =
        OrderDeliveryMethod.AddressDelivery;

    public int? KitchenSubscriptionId { get; set; }

    public KitchenSubscription? KitchenSubscription { get; set; }

    public string DeliveryFullName { get; set; } = string.Empty;

    public string DeliveryPhoneNumber { get; set; } = string.Empty;

    public string DeliveryAddressLine { get; set; } = string.Empty;

    public string DeliveryDistrict { get; set; } = string.Empty;

    public string DeliveryCity { get; set; } = string.Empty;

    public string? DeliveryPostalCode { get; set; }

    public DateOnly? DeliveryDate { get; set; }

    public string? DeliveryTimeSlot { get; set; }

    public string? Notes { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DeliveryFee { get; set; }

    public decimal Total { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? StockRestoredAtUtc { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];

    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = [];
}
