namespace NO23.Web.ViewModels.Member;

public class MemberOrderListItemViewModel
{
    public string OrderNumber { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string PaymentStatus { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime CreatedAtLocal { get; init; }

    public DateOnly DeliveryDate { get; init; }

    public string DeliveryTimeSlot { get; init; } = string.Empty;

    public decimal Total { get; init; }

    public int ItemCount { get; init; }

    public IReadOnlyList<MemberOrderItemViewModel> Items { get; init; } = [];
}
