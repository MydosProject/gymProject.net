using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class KitchenProductionPlanningServiceTests
{
    [Fact]
    public async Task CreateOrRefreshPlan_IncludesOnlyPaidActiveKitchenOrders()
    {
        await using var dbContext = CreateDbContext();
        var planDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var menuItem = new KitchenMenuItem
        {
            Name = "Chicken Bowl",
            Category = MenuItemCategory.MainMeal,
            UnitPrice = 250,
            Calories = 500,
            Ingredients = "Chicken, rice"
        };
        var ingredient = new KitchenIngredient
        {
            Name = "Chicken",
            Unit = KitchenIngredientUnit.Gram,
            CurrentStockQuantity = 5000,
            MinimumStockQuantity = 1000
        };
        dbContext.KitchenMenuItems.Add(menuItem);
        dbContext.KitchenIngredients.Add(ingredient);
        dbContext.KitchenRecipeIngredients.Add(new KitchenRecipeIngredient
        {
            KitchenMenuItem = menuItem,
            KitchenIngredient = ingredient,
            QuantityPerPortion = 100
        });

        AddKitchenOrder(dbContext, menuItem, planDate, OrderStatus.Confirmed, PaymentStatus.Paid, 1);
        AddKitchenOrder(dbContext, menuItem, planDate, OrderStatus.Preparing, PaymentStatus.Paid, 2);
        AddKitchenOrder(dbContext, menuItem, planDate, OrderStatus.OutForDelivery, PaymentStatus.Paid, 3);
        AddKitchenOrder(dbContext, menuItem, planDate, OrderStatus.Pending, PaymentStatus.Paid, 10);
        AddKitchenOrder(dbContext, menuItem, planDate, OrderStatus.Confirmed, PaymentStatus.Pending, 10);
        AddKitchenOrder(dbContext, menuItem, planDate, OrderStatus.Confirmed, PaymentStatus.Failed, 10);
        AddKitchenOrder(dbContext, menuItem, planDate, OrderStatus.Delivered, PaymentStatus.Paid, 10);
        AddKitchenOrder(dbContext, menuItem, planDate, OrderStatus.Cancelled, PaymentStatus.Refunded, 10);
        AddKitchenOrder(
            dbContext,
            menuItem,
            planDate.AddDays(1),
            OrderStatus.Confirmed,
            PaymentStatus.Paid,
            10);
        await dbContext.SaveChangesAsync();
        var service = new KitchenProductionPlanningService(dbContext);

        var result = await service.CreateOrRefreshPlanAsync(planDate);

        Assert.True(result.Succeeded);
        var plan = await dbContext.KitchenProductionPlans
            .Include(item => item.Items)
            .Include(item => item.Materials)
            .SingleAsync(item => item.PlanDate == planDate);
        var planItem = Assert.Single(plan.Items);
        Assert.Equal(6, planItem.OrderPortions);
        Assert.Equal(6, planItem.TotalPortions);
        var material = Assert.Single(plan.Materials);
        Assert.Equal(600, material.RequiredQuantity);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void AddKitchenOrder(
        ApplicationDbContext dbContext,
        KitchenMenuItem menuItem,
        DateOnly deliveryDate,
        OrderStatus status,
        PaymentStatus paymentStatus,
        int quantity)
    {
        dbContext.Orders.Add(new Order
        {
            OrderNumber = Guid.NewGuid().ToString("N"),
            Status = status,
            PaymentStatus = paymentStatus,
            DeliveryFullName = "NO23 Member",
            DeliveryPhoneNumber = "5551112233",
            DeliveryAddressLine = "Studio",
            DeliveryDistrict = "Kadikoy",
            DeliveryCity = "Istanbul",
            DeliveryDate = deliveryDate,
            DeliveryTimeSlot = "10:00-12:00",
            Total = quantity * menuItem.UnitPrice,
            Items =
            [
                new OrderItem
                {
                    ItemType = CartItemType.KitchenMenuItem,
                    KitchenMenuItem = menuItem,
                    ProductName = menuItem.Name,
                    UnitPrice = menuItem.UnitPrice,
                    Quantity = quantity,
                    LineTotal = quantity * menuItem.UnitPrice
                }
            ]
        });
    }
}
