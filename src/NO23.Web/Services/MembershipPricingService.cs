using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Services;

public class MembershipPricingService(ApplicationDbContext dbContext)
{
    public async Task<MembershipDiscounts> GetAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return new();
        var membershipId = await dbContext.MemberProfiles.AsNoTracking().Where(x => x.ApplicationUserId == userId)
            .Select(x => (int?)x.MembershipPackageId).FirstOrDefaultAsync();
        if (membershipId is null) return new();
        return await dbContext.ServicePackages.AsNoTracking()
            .Where(x => x.Category == ServicePackageCategory.Membership && x.IsActive && x.MembershipPackageId == membershipId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id)
            .Select(x => new MembershipDiscounts(x.KitchenDiscountPercent, x.CoffeeDiscountPercent, x.ShopDiscountPercent))
            .FirstOrDefaultAsync() ?? new();
    }
}

public record MembershipDiscounts(int Kitchen = 0, int Coffee = 0, int Shop = 0)
{
    public int For(CartItemType type) => type switch
    {
        CartItemType.KitchenSubscriptionPackage => Kitchen,
        CartItemType.KitchenMenuItem => Coffee,
        CartItemType.ShopProduct => Shop,
        _ => 0
    };
    public static decimal Apply(decimal price, int percent) => decimal.Round(price * (100 - Math.Clamp(percent, 0, 100)) / 100, 2, MidpointRounding.AwayFromZero);
    public IReadOnlyList<CartItemViewModel> ApplyTo(IEnumerable<CartItemViewModel> items) => items.Select(item =>
    {
        var percent = Enum.TryParse<CartItemType>(item.ItemType, out var type) ? For(type) : 0;
        var price = Apply(item.UnitPrice, percent);
        return new CartItemViewModel
        {
            Id = item.Id, ItemType = item.ItemType, ProductName = item.ProductName,
            RemovedIngredientNames = item.RemovedIngredientNames, AddedIngredientNames = item.AddedIngredientNames,
            UnitPrice = price, Quantity = item.Quantity, LineTotal = price * item.Quantity, DiscountPercent = percent
        };
    }).ToList();
}
