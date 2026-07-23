using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Services;

public class MemberCartQueryService(ApplicationDbContext dbContext)
{
    public async Task<int> GetItemCountAsync(string userId)
    {
        return await dbContext.CartItems
            .AsNoTracking()
            .Where(item =>
                item.ShoppingCart.MemberProfile.ApplicationUserId == userId)
            .SumAsync(item => (int?)item.Quantity) ?? 0;
    }

    public async Task<MemberCartPanelViewModel> BuildPanelAsync(
        string userId,
        CheckoutInputViewModel? checkoutInput = null)
    {
        var items = await dbContext.CartItems
            .AsNoTracking()
            .Where(item =>
                item.ShoppingCart.MemberProfile.ApplicationUserId == userId)
            .OrderBy(item => item.CreatedAtUtc)
            .Select(item => new CartItemViewModel
            {
                Id = item.Id,
                ItemType = item.ItemType.ToString(),
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                LineTotal = item.UnitPrice * item.Quantity
            })
            .ToListAsync();

        return new MemberCartPanelViewModel
        {
            CartItems = items,
            CheckoutInput = checkoutInput ?? new CheckoutInputViewModel()
        };
    }
}
