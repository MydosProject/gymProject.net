using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.GuestOrders;

namespace NO23.Tests;

public class KitchenOrderCustomizationTests
{
    [Fact]
    public async Task AddKitchenMenuItemToCart_PreservesCustomizationAndSeparatesDifferentSelections()
    {
        await using var dbContext = CreateDbContext();
        var data = await SeedAsync(dbContext);
        var service = new CommerceService(dbContext);

        var first = await service.AddKitchenMenuItemToCartAsync(
            data.UserId,
            data.MenuItemId,
            1,
            [data.RecipeIngredientId],
            [data.ExtraIngredientId]);
        var second = await service.AddKitchenMenuItemToCartAsync(
            data.UserId,
            data.MenuItemId,
            2,
            [data.RecipeIngredientId],
            [data.ExtraIngredientId]);
        var standard = await service.AddKitchenMenuItemToCartAsync(
            data.UserId,
            data.MenuItemId,
            1);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.True(standard.Succeeded);

        var cartItems = await dbContext.CartItems
            .OrderBy(item => item.Id)
            .ToListAsync();
        Assert.Equal(2, cartItems.Count);

        var customizedItem = cartItems[0];
        Assert.Equal(3, customizedItem.Quantity);
        Assert.Equal("Tavuk göğüs", customizedItem.RemovedIngredientNames);
        Assert.Equal("Avokado", customizedItem.AddedIngredientNames);
        Assert.Null(cartItems[1].RemovedIngredientNames);
        Assert.Null(cartItems[1].AddedIngredientNames);
    }

    [Fact]
    public async Task CreateOrderFromCart_CopiesKitchenCustomizationSnapshot()
    {
        await using var dbContext = CreateDbContext();
        var data = await SeedAsync(dbContext);
        var service = new CommerceService(dbContext);

        var addResult = await service.AddKitchenMenuItemToCartAsync(
            data.UserId,
            data.MenuItemId,
            1,
            [data.RecipeIngredientId],
            [data.ExtraIngredientId]);
        Assert.True(addResult.Succeeded);

        var orderResult = await service.CreateOneTimeOrderFromCartAsync(
            data.UserId,
            new DeliveryDetails
            {
                FullName = "Test Üye",
                PhoneNumber = "05555555555",
                AddressLine = "Test adresi",
                District = "Kadıköy",
                City = "İstanbul",
                DeliveryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                DeliveryTimeSlot = "10:00-13:00"
            });

        Assert.True(orderResult.Succeeded);
        var orderItem = Assert.Single(await dbContext.OrderItems.ToListAsync());
        Assert.Equal("Tavuk göğüs", orderItem.RemovedIngredientNames);
        Assert.Equal("Avokado", orderItem.AddedIngredientNames);
    }

    [Fact]
    public async Task CreateGuestKitchenOrder_RejectsIngredientsOutsideAllowedGroups()
    {
        await using var dbContext = CreateDbContext();
        var data = await SeedAsync(dbContext);
        var service = new CommerceService(dbContext);
        var input = BuildGuestInput();
        input.RemovedKitchenIngredientIds = [data.ExtraIngredientId];

        var invalidRemoval = await service.CreateGuestKitchenOrderAsync(
            data.MenuItemId,
            1,
            input);

        Assert.False(invalidRemoval.Succeeded);
        Assert.Contains("reçetesindeki", invalidRemoval.ErrorMessage);
        Assert.Empty(dbContext.Orders);

        input.RemovedKitchenIngredientIds = [];
        input.AddedKitchenIngredientIds = [data.RecipeIngredientId];

        var invalidAddition = await service.CreateGuestKitchenOrderAsync(
            data.MenuItemId,
            1,
            input);

        Assert.False(invalidAddition.Succeeded);
        Assert.Contains("zaten bulunan", invalidAddition.ErrorMessage);
        Assert.Empty(dbContext.Orders);
    }

    private static GuestOrderInputViewModel BuildGuestInput() => new()
    {
        Quantity = 1,
        FullName = "Test Misafir",
        Email = "guest@example.com",
        PhoneNumber = "05555555555",
        AddressLine = "Test adresi",
        District = "Kadıköy",
        City = "İstanbul",
        DeliveryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        DeliveryTimeSlot = "10:00-13:00"
    };

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<(
        string UserId,
        int MenuItemId,
        int RecipeIngredientId,
        int ExtraIngredientId)> SeedAsync(ApplicationDbContext dbContext)
    {
        const string userId = "kitchen-customization-member";
        var profile = new MemberProfile
        {
            ApplicationUserId = userId,
            MembershipPackageId = 1
        };
        var recipeIngredient = new KitchenIngredient
        {
            Name = "Tavuk göğüs",
            IsActive = true
        };
        var extraIngredient = new KitchenIngredient
        {
            Name = "Avokado",
            IsActive = true
        };
        var menuItem = new KitchenMenuItem
        {
            Name = "Protein Bowl",
            Category = MenuItemCategory.MainMeal,
            Calories = 450,
            UnitPrice = 300,
            Ingredients = "Tavuk göğüs",
            IsActive = true
        };
        menuItem.RecipeIngredients.Add(new KitchenRecipeIngredient
        {
            KitchenIngredient = recipeIngredient,
            QuantityPerPortion = 160
        });

        dbContext.MemberProfiles.Add(profile);
        dbContext.KitchenIngredients.Add(extraIngredient);
        dbContext.KitchenMenuItems.Add(menuItem);
        await dbContext.SaveChangesAsync();

        return (
            userId,
            menuItem.Id,
            recipeIngredient.Id,
            extraIngredient.Id);
    }
}
