using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class PaymentTransaction
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public string Provider { get; set; } = "iyzico";

    public string ConversationId { get; set; } = string.Empty;

    public string BasketId { get; set; } = string.Empty;

    public string? Token { get; set; }

    public string? PaymentPageUrl { get; set; }

    public string? PaymentId { get; set; }

    public string? RawStatus { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public int? FraudStatus { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public DateTime? CallbackReceivedAtUtc { get; set; }

    public DateTime? WebhookReceivedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime? FailedAtUtc { get; set; }

    public string? LastError { get; set; }

    public string? RawInitializeResponseJson { get; set; }

    public string? RawRetrieveResponseJson { get; set; }

    public string? RawWebhookJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}