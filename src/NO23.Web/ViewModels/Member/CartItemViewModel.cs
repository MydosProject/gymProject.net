namespace NO23.Web.ViewModels.Member;

public class CartItemViewModel
{
    public int Id { get; init; }

    public string ItemType { get; init; } = string.Empty;

    public string ProductName { get; init; } = string.Empty;

    public string? RemovedIngredientNames { get; init; }

    public string? AddedIngredientNames { get; init; }

    public decimal UnitPrice { get; init; }

    public int Quantity { get; init; }

    public decimal LineTotal { get; init; }
}
