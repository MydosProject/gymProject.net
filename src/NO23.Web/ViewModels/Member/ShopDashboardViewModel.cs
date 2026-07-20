namespace NO23.Web.ViewModels.Member;

public class ShopDashboardViewModel
{
    public IReadOnlyList<ShopProductCardViewModel> ShopProducts { get; init; } = [];

    public IReadOnlyList<KitchenMenuItemCardViewModel> KitchenMenuItems { get; init; } = [];

    public IReadOnlyList<CartItemViewModel> CartItems { get; init; } = [];

    public CheckoutInputViewModel CheckoutInput { get; init; } = new();

    public decimal CartSubtotal => CartItems.Sum(item => item.LineTotal);
}
