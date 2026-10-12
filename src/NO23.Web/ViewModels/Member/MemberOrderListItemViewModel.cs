namespace NO23.Web.ViewModels.Member;

public class MemberOrderListItemViewModel
{
    public string OrderNumber { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string PaymentStatus { get; init; } = string.Empty;

    public string PaymentMethod { get; init; } = "CreditCard";

    public bool IsManualPayment { get; init; }

    public IReadOnlyList<MemberOrderPaymentInstallmentViewModel> PaymentInstallments { get; init; } = [];

    public DateTime CreatedAtUtc { get; init; }

    public DateTime CreatedAtLocal { get; init; }

    public DateOnly? DeliveryDate { get; init; }

    public string? DeliveryTimeSlot { get; init; }

    public string DeliveryMethod { get; init; } = string.Empty;

    public decimal Total { get; init; }

    public string? DiscountCode { get; init; }

    public int CampaignDiscountPercent { get; init; }

    public decimal DiscountAmount { get; init; }

    public int ItemCount { get; init; }

    public IReadOnlyList<MemberOrderItemViewModel> Items { get; init; } = [];
}

public class MemberOrderPaymentInstallmentViewModel
{
    public int Number { get; init; }
    public decimal Amount { get; init; }
    public DateOnly DueDate { get; init; }
    public DateOnly? PaidOn { get; init; }
}
