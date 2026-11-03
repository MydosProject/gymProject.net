using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Areas.Admin.Controllers;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;

namespace NO23.Tests;

public class AdminOrderDeliveryDetailsTests
{
    [Fact]
    public async Task OrderList_ShowsRecipientAndAddressOnlyForAddressDelivery()
    {
        await using var db = CreateDbContext();
        db.Orders.AddRange(
            Order("NO23-ADDRESS", OrderType.OneTime, OrderDeliveryMethod.AddressDelivery, "Test adresi", "Bandırma", "Balıkesir"),
            Order("NO23-PICKUP", OrderType.OneTime, OrderDeliveryMethod.ClubPickup, "NO23 Sports Club", "NO23 Sports Club", "NO23 Sports Club"),
            Order("NO23-PACKAGE", OrderType.ServicePackagePurchase, OrderDeliveryMethod.AddressDelivery, "Kulüp paket kaydı", "-", "-"));
        await db.SaveChangesAsync();

        var result = await new OrdersController(db, new OrderWorkflowService(db)).Index();
        var orders = Assert.IsAssignableFrom<IEnumerable<OrderListItemViewModel>>(Assert.IsType<ViewResult>(result).Model).ToList();

        var address = orders.Single(x => x.OrderNumber == "NO23-ADDRESS");
        Assert.Equal("Test Alıcı", address.RecipientName);
        Assert.Equal("05000000000", address.RecipientPhone);
        Assert.Equal("Test adresi, Bandırma / Balıkesir", address.DeliveryAddress);
        var pickup = orders.Single(x => x.OrderNumber == "NO23-PICKUP");
        Assert.Equal("Test Alıcı", pickup.RecipientName);
        Assert.Null(pickup.DeliveryAddress);
        var package = orders.Single(x => x.OrderNumber == "NO23-PACKAGE");
        Assert.Null(package.RecipientName);
        Assert.Null(package.DeliveryAddress);
    }

    private static Order Order(string number, OrderType type, OrderDeliveryMethod method, string address, string district, string city) => new()
    {
        OrderNumber = number, Type = type, DeliveryMethod = method, DeliveryFullName = "Test Alıcı", DeliveryPhoneNumber = "05000000000",
        DeliveryAddressLine = address, DeliveryDistrict = district, DeliveryCity = city, Total = 100
    };

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
