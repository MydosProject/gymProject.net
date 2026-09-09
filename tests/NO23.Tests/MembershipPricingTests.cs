using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.Services.Payments;

namespace NO23.Tests;

public class MembershipPricingTests
{
    [Theory]
    [InlineData("hybrid", 5, 5, 0)]
    [InlineData("pro-membership", 10, 10, 5)]
    [InlineData("black", 15, 10, 5)]
    public async Task MemberDiscounts_MatchCatalogAndCheckout(string slug, int kitchen, int coffee, int shop)
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var package = ServicePackageSeed.Defaults.Single(x => x.Slug == slug);
        db.ServicePackages.Add(package);
        var member = new MemberProfile
        { ApplicationUser = new ApplicationUser { UserName = "pricing-test", Email = "pricing@example.test" }, MembershipPackage = package.MembershipPackage! };
        db.MemberProfiles.Add(member);
        var product = new ShopProduct { Name = "Tişört", Sku = "T-1", UnitPrice = 100, StockQuantity = 10, IsActive = true };
        var meal = new KitchenMenuItem { Name = "Öğle", UnitPrice = 100, IsActive = true };
        db.ShopProducts.Add(product);
        db.KitchenMenuItems.Add(meal);
        await db.SaveChangesAsync();
        var userId = member.ApplicationUserId;
        var discounts = await new MembershipPricingService(db).GetAsync(userId);
        Assert.Equal(new MembershipDiscounts(kitchen, coffee, shop), discounts);
        Assert.Equal(100 - kitchen, MembershipDiscounts.Apply(100, discounts.Kitchen));
        Assert.Equal(new MembershipDiscounts(), await new MembershipPricingService(db).GetAsync(null));

        var commerce = new CommerceService(db);
        Assert.True((await commerce.AddShopProductToCartAsync(userId, product.Id, 2)).Succeeded);
        Assert.True((await commerce.AddKitchenMenuItemToCartAsync(userId, meal.Id, 1)).Succeeded);
        var queries = new MemberCartQueryService(db, Options.Create(new IyzicoOptions()), Options.Create(new ClubPickupOptions()));
        var panel = await queries.BuildPanelAsync(userId);
        var expected = 2 * (100 - shop) + (100 - coffee);
        Assert.Equal(expected, panel.CartItems.Sum(x => x.LineTotal));
        // A second read must not apply discounts a second time to saved cart prices.
        Assert.Equal(expected, (await queries.BuildPanelAsync(userId)).CartItems.Sum(x => x.LineTotal));
        Assert.All(await db.CartItems.ToListAsync(), x => Assert.Equal(100, x.UnitPrice));

        var result = await commerce.CreateOneTimeOrderFromCartAsync(userId, new DeliveryDetails
        { FullName = "Test", PhoneNumber = "05555555555", AddressLine = "Test adres", City = "İstanbul", District = "Kadıköy" });
        Assert.True(result.Succeeded);
        var order = await db.Orders.Include(x => x.Items).SingleAsync();
        Assert.Equal(expected, order.Total);
        Assert.Equal(panel.CartItems.Select(x => x.UnitPrice).Order(), order.Items.Select(x => x.UnitPrice).Order());
    }
}
