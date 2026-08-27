using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.GuestOrders;
using NO23.Web.ViewModels.Member;

namespace NO23.Tests;

public class OrderDeliveryMethodTests
{
    [Fact]
    public void AddressDelivery_RequiresAddressCityAndDistrict()
    {
        var input = ValidInput();
        input.AddressLine = string.Empty;
        input.City = string.Empty;
        input.District = string.Empty;

        var errors = Validate(input);

        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(input.AddressLine)));
        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(input.City)));
        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(input.District)));
    }

    [Fact]
    public void ClubPickup_DoesNotRequireCustomerAddress()
    {
        var input = ValidInput();
        input.DeliveryMethod = OrderDeliveryMethod.ClubPickup;
        input.AddressLine = string.Empty;
        input.City = string.Empty;
        input.District = string.Empty;

        Assert.Empty(Validate(input));
    }

    [Fact]
    public void KitchenPackageAddressDelivery_RequiresAddressCityAndDistrict()
    {
        var input = ValidKitchenCheckoutInput();
        input.AddressLine = string.Empty;
        input.City = string.Empty;
        input.District = string.Empty;

        var errors = Validate(input);

        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(input.AddressLine)));
        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(input.City)));
        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(input.District)));
    }

    [Fact]
    public void KitchenPackageClubPickup_DoesNotRequireCustomerAddress()
    {
        var input = ValidKitchenCheckoutInput();
        input.DeliveryMethod = OrderDeliveryMethod.ClubPickup;
        input.AddressLine = string.Empty;
        input.City = string.Empty;
        input.District = string.Empty;

        Assert.Empty(Validate(input));
    }

    [Fact]
    public async Task ClubPickup_WhenAddressIsNotConfigured_UsesSingleClubFallback()
    {
        await using var dbContext = CreateDbContext();
        var product = await SeedProductAsync(dbContext);
        var service = new CommerceService(
            dbContext,
            Options.Create(new ClubPickupOptions()));
        var input = ValidInput();
        input.DeliveryMethod = OrderDeliveryMethod.ClubPickup;

        var result = await service.CreateGuestShopOrderAsync(
            product.Id,
            1,
            input);

        Assert.True(result.Succeeded);
        var order = await dbContext.Orders.SingleAsync();
        Assert.Equal(OrderDeliveryMethod.ClubPickup, order.DeliveryMethod);
        Assert.Equal("NO23 Sports Club", order.DeliveryAddressLine);
        Assert.Equal("NO23 Sports Club", order.DeliveryDistrict);
        Assert.Equal("NO23 Sports Club", order.DeliveryCity);
        Assert.Equal(4, product.StockQuantity);
    }

    [Fact]
    public async Task ClubPickup_UsesConfiguredClubAddressAndPersistsMethod()
    {
        await using var dbContext = CreateDbContext();
        var product = await SeedProductAsync(dbContext);
        var service = new CommerceService(
            dbContext,
            Options.Create(new ClubPickupOptions
            {
                DisplayName = "NO23 Test Club",
                AddressLine = "Test Mahallesi 23",
                District = "Test İlçesi",
                City = "Test Şehri"
            }));
        var input = ValidInput();
        input.DeliveryMethod = OrderDeliveryMethod.ClubPickup;
        input.AddressLine = string.Empty;
        input.District = string.Empty;
        input.City = string.Empty;

        var result = await service.CreateGuestShopOrderAsync(
            product.Id,
            1,
            input);

        Assert.True(result.Succeeded);
        var order = await dbContext.Orders.SingleAsync();
        Assert.Equal(OrderDeliveryMethod.ClubPickup, order.DeliveryMethod);
        Assert.Equal("Test Mahallesi 23", order.DeliveryAddressLine);
        Assert.Equal("Test İlçesi", order.DeliveryDistrict);
        Assert.Equal("Test Şehri", order.DeliveryCity);
    }

    private static GuestOrderInputViewModel ValidInput() => new()
    {
        DeliveryMethod = OrderDeliveryMethod.AddressDelivery,
        FullName = "Test Kullanıcı",
        Email = "test@example.com",
        PhoneNumber = "05555555555",
        AddressLine = "Test adresi",
        District = "Test ilçesi",
        City = "Test şehri",
        DeliveryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        DeliveryTimeSlot = "10:00-13:00"
    };

    private static KitchenCheckoutViewModel ValidKitchenCheckoutInput() => new()
    {
        KitchenSubscriptionId = 1,
        DeliveryMethod = OrderDeliveryMethod.AddressDelivery,
        FullName = "Test Kullanıcı",
        PhoneNumber = "05555555555",
        AddressLine = "Test adresi",
        District = "Test ilçesi",
        City = "Test şehri"
    };

    private static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            errors,
            validateAllProperties: true);
        return errors;
    }

    private static async Task<ShopProduct> SeedProductAsync(
        ApplicationDbContext dbContext)
    {
        var product = new ShopProduct
        {
            Name = "Test Ürün",
            Sku = $"TEST-{Guid.NewGuid():N}",
            Category = "Test",
            UnitPrice = 100,
            StockQuantity = 5,
            IsActive = true
        };
        dbContext.ShopProducts.Add(product);
        await dbContext.SaveChangesAsync();
        return product;
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
