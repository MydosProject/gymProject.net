namespace NO23.Web.Services.Payments;

public enum IyzicoCheckoutItemType
{
    Physical = 1,
    Virtual = 2
}

public sealed class IyzicoCheckoutInitializeRequest
{
    public required string ConversationId { get; init; }

    public required string BasketId { get; init; }

    public string CallbackUrl { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public decimal PaidPrice { get; init; }

    public required IyzicoCheckoutBuyer Buyer { get; init; }

    public required IyzicoCheckoutAddress ShippingAddress { get; init; }

    public required IyzicoCheckoutAddress BillingAddress { get; init; }

    public IReadOnlyList<IyzicoCheckoutItem> Items { get; init; } = [];
}

public sealed class IyzicoCheckoutBuyer
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Surname { get; init; }

    public required string IdentityNumber { get; init; }

    public required string Email { get; init; }

    public required string GsmNumber { get; init; }

    public required string RegistrationAddress { get; init; }

    public required string City { get; init; }

    public string Country { get; init; } = "Turkey";

    public string? ZipCode { get; init; }

    public required string IpAddress { get; init; }

    public DateTime RegistrationDateUtc { get; init; }

    public DateTime LastLoginDateUtc { get; init; }
}

public sealed class IyzicoCheckoutAddress
{
    public required string ContactName { get; init; }

    public required string Description { get; init; }

    public required string City { get; init; }

    public string Country { get; init; } = "Turkey";

    public string? ZipCode { get; init; }
}

public sealed class IyzicoCheckoutItem
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Category1 { get; init; }

    public string? Category2 { get; init; }

    public decimal Price { get; init; }

    public IyzicoCheckoutItemType ItemType { get; init; } =
        IyzicoCheckoutItemType.Physical;
}

public sealed class IyzicoCheckoutInitializeResult
{
    public bool Succeeded { get; init; }

    public int StatusCode { get; init; }

    public string? ConversationId { get; init; }

    public string? RawStatus { get; init; }

    public string? Token { get; init; }

    public long? TokenExpireTime { get; init; }

    public string? PaymentPageUrl { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ErrorGroup { get; init; }

    public string? RawResponseJson { get; init; }
}

public sealed class IyzicoCheckoutRetrieveResult
{
    public bool Succeeded { get; init; }

    public int StatusCode { get; init; }

    public string? ConversationId { get; init; }

    public string? RawStatus { get; init; }

    public string? Token { get; init; }

    public string? PaymentId { get; init; }

    public string? PaymentStatus { get; init; }

    public int? FraudStatus { get; init; }

    public string? BasketId { get; init; }

    public string? Price { get; init; }

    public string? PaidPrice { get; init; }

    public string? Currency { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ErrorGroup { get; init; }

    public string? RawResponseJson { get; init; }
}