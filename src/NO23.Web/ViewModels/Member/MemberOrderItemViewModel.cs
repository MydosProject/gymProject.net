namespace NO23.Web.ViewModels.Member;

public class MemberOrderItemViewModel
{
    public string ProductName { get; init; } = string.Empty;

    public string ItemType { get; init; } = string.Empty;

    public decimal UnitPrice { get; init; }

    public int Quantity { get; init; }

    public decimal LineTotal { get; init; }
}
