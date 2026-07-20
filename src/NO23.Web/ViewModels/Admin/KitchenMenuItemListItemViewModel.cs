namespace NO23.Web.ViewModels.Admin;

public class KitchenMenuItemListItemViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public int Calories { get; init; }

    public decimal ProteinGrams { get; init; }

    public decimal CarbohydrateGrams { get; init; }

    public decimal FatGrams { get; init; }

    public string? Tags { get; init; }

    public bool IsActive { get; init; }

    public int DisplayOrder { get; init; }
}
