using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.GuestOrders;

namespace NO23.Tests;

public class ShopProductVariantTests
{
    [Fact]
    public async Task CreateGuestShopOrderAsync_UsesSelectedVariantStockAndSnapshot()
    {
        await using var dbContext = CreateDbContext();
        var product = new ShopProduct
        {
            Name = "NO23 Tişört",
            Sku = "TSHIRT-23",
            Category = "Apparel",
            UnitPrice = 900,
            StockQuantity = 3,
            Variants =
            [
                new ShopProductVariant
                {
                    Size = "M",
                    StockQuantity = 2,
                    IsActive = true,
                    DisplayOrder = 1
                },
                new ShopProductVariant
                {
                    Size = "L",
                    StockQuantity = 1,
                    IsActive = true,
                    DisplayOrder = 2
                }
            ]
        };
        dbContext.ShopProducts.Add(product);
        await dbContext.SaveChangesAsync();
        var selectedVariant = product.Variants.Single(item => item.Size == "M");
        var service = new CommerceService(dbContext);
        var input = ValidGuestInput();
        input.ShopProductVariantId = selectedVariant.Id;

        var result = await service.CreateGuestShopOrderAsync(
            product.Id,
            1,
            input);

        Assert.True(result.Succeeded);
        Assert.Equal(2, product.StockQuantity);
        Assert.Equal(1, selectedVariant.StockQuantity);
        var orderItem = await dbContext.OrderItems.SingleAsync();
        Assert.Equal(selectedVariant.Id, orderItem.ShopProductVariantId);
        Assert.Equal("M", orderItem.SelectedSize);
        Assert.Equal("NO23 Tişört · M", orderItem.ProductName);

        var order = await dbContext.Orders
            .Include(item => item.Items)
            .ThenInclude(item => item.ShopProduct)
            .Include(item => item.Items)
            .ThenInclude(item => item.ShopProductVariant)
            .SingleAsync();
        OrderWorkflowService.RestoreShopProductStockOnce(order);
        OrderWorkflowService.RestoreShopProductStockOnce(order);

        Assert.Equal(3, product.StockQuantity);
        Assert.Equal(2, selectedVariant.StockQuantity);
    }

    [Fact]
    public async Task AddShopProductToCartAsync_RequiresVariantForSizedProduct()
    {
        await using var dbContext = CreateDbContext();
        var profile = SeedMember(dbContext);
        var product = new ShopProduct
        {
            Name = "NO23 Sweatshirt",
            Sku = "SWEAT-23",
            Category = "Apparel",
            UnitPrice = 1500,
            StockQuantity = 4,
            Variants =
            [
                new ShopProductVariant
                {
                    Size = "XL",
                    StockQuantity = 4,
                    IsActive = true,
                    DisplayOrder = 1
                }
            ]
        };
        dbContext.ShopProducts.Add(product);
        await dbContext.SaveChangesAsync();
        var service = new CommerceService(dbContext);

        var missingVariantResult = await service.AddShopProductToCartAsync(
            profile.ApplicationUserId,
            product.Id,
            1);
        var variant = product.Variants.Single();
        var selectedVariantResult = await service.AddShopProductToCartAsync(
            profile.ApplicationUserId,
            product.Id,
            1,
            variant.Id);

        Assert.False(missingVariantResult.Succeeded);
        Assert.True(selectedVariantResult.Succeeded);
        var cartItem = await dbContext.CartItems.SingleAsync();
        Assert.Equal(variant.Id, cartItem.ShopProductVariantId);
        Assert.Equal("XL", cartItem.SelectedSize);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static MemberProfile SeedMember(ApplicationDbContext dbContext)
    {
        var profile = new MemberProfile
        {
            ApplicationUser = new ApplicationUser
            {
                UserName = "variant-member@no23.test",
                Email = "variant-member@no23.test"
            },
            MembershipPackage = new MembershipPackage
            {
                Code = MembershipPackageCode.Pro,
                Name = "Pro",
                Audience = "Test",
                Description = "Test paketi",
                WeeklyClassLimit = 4
            },
            RemainingClassCredits = 4
        };
        dbContext.MemberProfiles.Add(profile);
        return profile;
    }

    private static GuestOrderInputViewModel ValidGuestInput() => new()
    {
        Quantity = 1,
        FullName = "Test Kullanıcı",
        Email = "guest@no23.test",
        PhoneNumber = "05550000000",
        AddressLine = "Test Mahallesi No: 23",
        District = "Kadıköy",
        City = "İstanbul",
        DeliveryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        DeliveryTimeSlot = "10:00-13:00"
    };
}
