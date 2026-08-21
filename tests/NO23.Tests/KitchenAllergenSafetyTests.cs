using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class KitchenAllergenSafetyTests
{
    [Fact]
    public async Task AddKitchenMenuItemToCart_BlocksMemberAllergenConflict()
    {
        await using var dbContext = CreateDbContext();
        var data = await SeedAsync(dbContext, memberHasAllergen: true);
        var service = new CommerceService(dbContext);

        var result = await service.AddKitchenMenuItemToCartAsync(data.UserId, data.MenuItemId, 1);

        Assert.False(result.Succeeded);
        Assert.Contains("Süt", result.ErrorMessage);
        Assert.Empty(dbContext.CartItems);
    }

    [Fact]
    public async Task AddKitchenMenuItemToCart_AllowsSafeMeal()
    {
        await using var dbContext = CreateDbContext();
        var data = await SeedAsync(dbContext, memberHasAllergen: false);
        var service = new CommerceService(dbContext);

        var result = await service.AddKitchenMenuItemToCartAsync(data.UserId, data.MenuItemId, 2);

        Assert.True(result.Succeeded);
        Assert.Equal(2, Assert.Single(dbContext.CartItems).Quantity);
    }

    [Fact]
    public async Task CreateOrder_RevalidatesAllergensChangedAfterCartAddition()
    {
        await using var dbContext = CreateDbContext();
        var data = await SeedAsync(dbContext, memberHasAllergen: false);
        var service = new CommerceService(dbContext);
        Assert.True((await service.AddKitchenMenuItemToCartAsync(data.UserId, data.MenuItemId, 1)).Succeeded);
        dbContext.MemberAllergens.Add(new MemberAllergen
        {
            MemberProfileId = data.MemberProfileId,
            KitchenAllergenId = data.AllergenId
        });
        await dbContext.SaveChangesAsync();

        var result = await service.CreateOneTimeOrderFromCartAsync(data.UserId, new DeliveryDetails
        {
            FullName = "Test User", PhoneNumber = "5555555555", AddressLine = "Test adresi",
            District = "Kadıköy", City = "İstanbul"
        });

        Assert.False(result.Succeeded);
        Assert.Contains("Süt", result.ErrorMessage);
        Assert.Empty(dbContext.Orders);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<(string UserId, int MemberProfileId, int MenuItemId, int AllergenId)> SeedAsync(
        ApplicationDbContext dbContext, bool memberHasAllergen)
    {
        const string userId = "member-1";
        var profile = new MemberProfile { ApplicationUserId = userId, MembershipPackageId = 1 };
        var allergen = new KitchenAllergen { Name = "Süt", DisplayOrder = 1 };
        var menuItem = new KitchenMenuItem
        {
            Name = "Yoğurt Kasesi", Category = MenuItemCategory.Breakfast, Calories = 300,
            UnitPrice = 200, Ingredients = "Yoğurt", IsActive = true
        };
        menuItem.MenuItemAllergens.Add(new KitchenMenuItemAllergen { KitchenAllergen = allergen });
        dbContext.MemberProfiles.Add(profile);
        dbContext.KitchenMenuItems.Add(menuItem);
        await dbContext.SaveChangesAsync();
        if (memberHasAllergen)
        {
            dbContext.MemberAllergens.Add(new MemberAllergen
            {
                MemberProfileId = profile.Id, KitchenAllergenId = allergen.Id
            });
            await dbContext.SaveChangesAsync();
        }
        return (userId, profile.Id, menuItem.Id, allergen.Id);
    }
}
