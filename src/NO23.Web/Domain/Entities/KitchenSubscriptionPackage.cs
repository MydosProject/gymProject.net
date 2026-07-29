using NO23.Web.Domain.Enums;

namespace NO23.Web.Domain.Entities;

public class KitchenSubscriptionPackage
{
    public int Id { get; set; }

    public KitchenSubscriptionPlan Plan { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Days { get; set; }

    public decimal UnitPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<KitchenSubscription> KitchenSubscriptions { get; set; } = [];
}
