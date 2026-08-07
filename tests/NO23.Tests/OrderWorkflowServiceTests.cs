using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Tests;

public class OrderWorkflowServiceTests
{
    [Fact]
    public async Task UpdateOrderStatus_DoesNotConfirmPendingOrderBeforePaymentIsPaid()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatus.Pending, PaymentStatus.Pending);
        var service = new OrderWorkflowService(dbContext);

        var result = await service.UpdateOrderStatusAsync(order.Id, OrderStatus.Confirmed);

        Assert.False(result.Succeeded);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public async Task UpdateOrderStatus_ConfirmsPendingOrderWhenPaymentIsPaid()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatus.Pending, PaymentStatus.Paid);
        var service = new OrderWorkflowService(dbContext);

        var result = await service.UpdateOrderStatusAsync(order.Id, OrderStatus.Confirmed);

        Assert.True(result.Succeeded);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task UpdateOrderStatus_RejectsOutOfSequenceTransition()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatus.Confirmed, PaymentStatus.Paid);
        var service = new OrderWorkflowService(dbContext);

        var result = await service.UpdateOrderStatusAsync(order.Id, OrderStatus.OutForDelivery);

        Assert.False(result.Succeeded);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task UpdateOrderStatus_RejectsChangesFromTerminalStatuses(OrderStatus terminalStatus)
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, terminalStatus, PaymentStatus.Paid);
        var service = new OrderWorkflowService(dbContext);

        var result = await service.UpdateOrderStatusAsync(order.Id, OrderStatus.Preparing);

        Assert.False(result.Succeeded);
        Assert.Equal(terminalStatus, order.Status);
    }

    [Fact]
    public async Task UpdatePaymentStatus_RefundsPaidOrderAndCancelsIt()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatus.Confirmed, PaymentStatus.Paid);
        var service = new OrderWorkflowService(dbContext);

        var result = await service.UpdatePaymentStatusAsync(order.Id, PaymentStatus.Refunded);

        Assert.True(result.Succeeded);
        Assert.Equal(PaymentStatus.Refunded, order.PaymentStatus);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public async Task UpdatePaymentStatus_DoesNotRefundDeliveredOrder()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatus.Delivered, PaymentStatus.Paid);
        var service = new OrderWorkflowService(dbContext);

        var result = await service.UpdatePaymentStatusAsync(order.Id, PaymentStatus.Refunded);

        Assert.False(result.Succeeded);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public async Task UpdatePaymentStatus_RejectsChangesFromRefundedPayment()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatus.Cancelled, PaymentStatus.Refunded);
        var service = new OrderWorkflowService(dbContext);

        var result = await service.UpdatePaymentStatusAsync(order.Id, PaymentStatus.Paid);

        Assert.False(result.Succeeded);
        Assert.Equal(PaymentStatus.Refunded, order.PaymentStatus);
    }

    [Fact]
    public async Task UpdateOrderStatus_RestoresShopProductStockWhenUnpaidOrderIsCancelled()
    {
        await using var dbContext = CreateDbContext();
        var product = new ShopProduct
        {
            Name = "Training Gloves",
            Sku = "GLV-001",
            Category = "Equipment",
            UnitPrice = 100,
            StockQuantity = 4
        };
        var order = BuildOrder(OrderStatus.Pending, PaymentStatus.Pending);
        order.Items.Add(new OrderItem
        {
            ItemType = CartItemType.ShopProduct,
            ShopProduct = product,
            ProductName = product.Name,
            UnitPrice = product.UnitPrice,
            Quantity = 2,
            LineTotal = 200
        });
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = new OrderWorkflowService(dbContext);

        var result = await service.UpdateOrderStatusAsync(order.Id, OrderStatus.Cancelled);

        Assert.True(result.Succeeded);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(6, product.StockQuantity);
    }

    [Fact]
    public async Task UpdatePaymentStatus_RestoresShopProductStockOnlyOnceWhenRefunded()
    {
        await using var dbContext = CreateDbContext();
        var product = new ShopProduct
        {
            Name = "Protein Shaker",
            Sku = "SHK-001",
            Category = "Equipment",
            UnitPrice = 50,
            StockQuantity = 8
        };
        var order = BuildOrder(OrderStatus.Confirmed, PaymentStatus.Paid);
        order.Items.Add(new OrderItem
        {
            ItemType = CartItemType.ShopProduct,
            ShopProduct = product,
            ProductName = product.Name,
            UnitPrice = product.UnitPrice,
            Quantity = 3,
            LineTotal = 150
        });
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = new OrderWorkflowService(dbContext);

        var firstResult = await service.UpdatePaymentStatusAsync(order.Id, PaymentStatus.Refunded);
        var secondResult = await service.UpdatePaymentStatusAsync(order.Id, PaymentStatus.Paid);

        Assert.True(firstResult.Succeeded);
        Assert.False(secondResult.Succeeded);
        Assert.Equal(11, product.StockQuantity);
        Assert.Equal(PaymentStatus.Refunded, order.PaymentStatus);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public async Task UpdatePaymentStatus_FailedPaymentCancelsOrderAndRestoresStockOnlyOnce()
        {
        await using var dbContext = CreateDbContext();

        var product = new ShopProduct
        {
            Name = "Training Band",
            Sku = "BAND-001",
            Category = "Equipment",
            UnitPrice = 75,
            StockQuantity = 5
        };

        var order = BuildOrder(
            OrderStatus.Pending,
            PaymentStatus.Pending);

        order.Items.Add(new OrderItem
        {
            ItemType = CartItemType.ShopProduct,
            ShopProduct = product,
            ProductName = product.Name,
            UnitPrice = product.UnitPrice,
            Quantity = 2,
            LineTotal = 150
        });

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var service = new OrderWorkflowService(dbContext);

        var firstResult = await service.UpdatePaymentStatusAsync(
            order.Id,
            PaymentStatus.Failed);

        var duplicateResult = await service.UpdatePaymentStatusAsync(
            order.Id,
            PaymentStatus.Failed);

        Assert.True(firstResult.Succeeded);
        Assert.True(duplicateResult.Succeeded);

        Assert.Equal(PaymentStatus.Failed, order.PaymentStatus);
        Assert.Equal(OrderStatus.Cancelled, order.Status);

        Assert.Equal(7, product.StockQuantity);
        Assert.NotNull(order.StockRestoredAtUtc);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<Order> SeedOrderAsync(
        ApplicationDbContext dbContext,
        OrderStatus status,
        PaymentStatus paymentStatus)
    {
        var order = BuildOrder(status, paymentStatus);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        return order;
    }

    private static Order BuildOrder(OrderStatus status, PaymentStatus paymentStatus)
    {
        return new Order
        {
            OrderNumber = Guid.NewGuid().ToString("N"),
            Status = status,
            PaymentStatus = paymentStatus,
            DeliveryFullName = "NO23 Member",
            DeliveryPhoneNumber = "5551112233",
            DeliveryAddressLine = "Studio",
            DeliveryDistrict = "Kadikoy",
            DeliveryCity = "Istanbul",
            DeliveryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            DeliveryTimeSlot = "10:00-12:00",
            Total = 100
        };
    }


}
