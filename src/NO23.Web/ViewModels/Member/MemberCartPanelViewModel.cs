namespace NO23.Web.ViewModels.Member;

public class MemberCartPanelViewModel
{
    public IReadOnlyList<CartItemViewModel> CartItems { get; init; } = [];

    public CheckoutInputViewModel CheckoutInput { get; init; } = new();

    public int TotalItemCount => CartItems.Sum(item => item.Quantity);

    public decimal CartSubtotal => CartItems.Sum(item => item.LineTotal);
}
