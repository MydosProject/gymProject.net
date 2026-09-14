namespace NO23.Web.ViewModels.Admin;

public class KitchenSubscriptionPackageListItemViewModel
{
    public int Id { get; init; }

    public string Plan { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int Days { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal TwoMainMealsPrice { get; init; }

    public decimal ThreeMainMealsPrice { get; init; }

    public decimal DailyDeliveryFee { get; init; }

    public bool IsActive { get; init; }

    public int DisplayOrder { get; init; }

    public int SubscriptionCount { get; init; }
}
