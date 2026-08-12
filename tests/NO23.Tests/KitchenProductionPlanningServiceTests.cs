using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Hubs;
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

    [Fact]
    public async Task UpdatePlanStatus_WhenCompletedAndStockBecomesCritical_PublishesKitchenCriticalNotification()
    {
        await using var dbContext = CreateDbContext();
        var service = await CreateServiceWithAdminNotificationsAsync(dbContext);
        var ingredient = new KitchenIngredient
        {
            Name = "Chicken",
            Unit = KitchenIngredientUnit.Gram,
            CurrentStockQuantity = 1200,
            MinimumStockQuantity = 1000
        };
        var plan = CreateReadyProductionPlan(
            ingredient,
            requiredQuantity: 300);
        dbContext.Add(plan);
        await dbContext.SaveChangesAsync();

        var result = await service.UpdatePlanStatusAsync(
            plan.Id,
            KitchenProductionPlanStatus.Completed);

        Assert.True(result.Succeeded);
        Assert.Equal(900, ingredient.CurrentStockQuantity);
        Assert.NotNull(plan.StockDeductedAtUtc);

        var notification = await dbContext.UserNotifications
            .SingleAsync();

        Assert.Equal(
            UserNotificationType.KitchenStockCritical,
            notification.Type);
        Assert.Equal(ingredient.Id, notification.RelatedEntityId);
        Assert.Contains("Chicken", notification.Message);
    }

    [Fact]
    public async Task UpdatePlanStatus_WhenCompletedAndStockBecomesOut_PublishesKitchenOutNotification()
    {
        await using var dbContext = CreateDbContext();
        var service = await CreateServiceWithAdminNotificationsAsync(dbContext);
        var ingredient = new KitchenIngredient
        {
            Name = "Chicken",
            Unit = KitchenIngredientUnit.Gram,
            CurrentStockQuantity = 300,
            MinimumStockQuantity = 1000
        };
        var plan = CreateReadyProductionPlan(
            ingredient,
            requiredQuantity: 300);
        dbContext.Add(plan);
        await dbContext.SaveChangesAsync();

        var result = await service.UpdatePlanStatusAsync(
            plan.Id,
            KitchenProductionPlanStatus.Completed);

        Assert.True(result.Succeeded);
        Assert.Equal(0, ingredient.CurrentStockQuantity);
        Assert.NotNull(plan.StockDeductedAtUtc);

        var notification = await dbContext.UserNotifications
            .SingleAsync();

        Assert.Equal(
            UserNotificationType.KitchenStockOut,
            notification.Type);
        Assert.Equal(ingredient.Id, notification.RelatedEntityId);
        Assert.Contains("Chicken", notification.Message);
    }

    [Fact]
    public async Task UpdatePlanStatus_WhenCompletedTwice_DoesNotDeductStockOrNotifyTwice()
    {
        await using var dbContext = CreateDbContext();
        var service = await CreateServiceWithAdminNotificationsAsync(dbContext);
        var ingredient = new KitchenIngredient
        {
            Name = "Chicken",
            Unit = KitchenIngredientUnit.Gram,
            CurrentStockQuantity = 1200,
            MinimumStockQuantity = 1000
        };
        var plan = CreateReadyProductionPlan(
            ingredient,
            requiredQuantity: 300);
        dbContext.Add(plan);
        await dbContext.SaveChangesAsync();

        var firstResult = await service.UpdatePlanStatusAsync(
            plan.Id,
            KitchenProductionPlanStatus.Completed);

        var secondResult = await service.UpdatePlanStatusAsync(
            plan.Id,
            KitchenProductionPlanStatus.Completed);

        Assert.True(firstResult.Succeeded);
        Assert.True(secondResult.Succeeded);
        Assert.Equal(900, ingredient.CurrentStockQuantity);
        Assert.Equal(1, await dbContext.UserNotifications.CountAsync());
        Assert.Equal(1, await dbContext.KitchenStockMovements.CountAsync());
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(
                    InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<KitchenProductionPlanningService>
        CreateServiceWithAdminNotificationsAsync(
            ApplicationDbContext dbContext)
    {
        var userStore =
            new UserStore<ApplicationUser>(dbContext);
        var roleStore =
            new RoleStore<IdentityRole>(dbContext);
        var userManager =
            new UserManager<ApplicationUser>(
                userStore,
                Options.Create(new IdentityOptions()),
                new PasswordHasher<ApplicationUser>(),
                [],
                [],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new EmptyServiceProvider(),
                NullLogger<UserManager<ApplicationUser>>.Instance);
        var roleManager =
            new RoleManager<IdentityRole>(
                roleStore,
                [],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                NullLogger<RoleManager<IdentityRole>>.Instance);

        await roleManager.CreateAsync(
            new IdentityRole(ApplicationRoles.Admin));

        var adminUser =
            new ApplicationUser
            {
                UserName = "admin@no23.test",
                Email = "admin@no23.test"
            };

        await userManager.CreateAsync(adminUser);
        await userManager.AddToRoleAsync(
            adminUser,
            ApplicationRoles.Admin);

        var notificationService =
            new UserNotificationService(dbContext);
        var realtimeService =
            new UserNotificationRealtimeService(
                notificationService,
                new NoOpUserNotificationHubContext());
        var stockNotificationService =
            new AdminStockNotificationService(
                userManager,
                realtimeService);

        return new KitchenProductionPlanningService(
            dbContext,
            stockNotificationService);
    }

    private static KitchenProductionPlan CreateReadyProductionPlan(
        KitchenIngredient ingredient,
        decimal requiredQuantity)
    {
        var menuItem =
            new KitchenMenuItem
            {
                Name = "Chicken Bowl",
                Category = MenuItemCategory.MainMeal,
                UnitPrice = 250,
                Calories = 500,
                Ingredients = "Chicken, rice"
            };

        return new KitchenProductionPlan
        {
            PlanDate =
                DateOnly.FromDateTime(
                    DateTime.Today.AddDays(1)),
            Status =
                KitchenProductionPlanStatus.InPreparation,
            Items =
            [
                new KitchenProductionPlanItem
                {
                    KitchenMenuItem = menuItem,
                    ProductNameSnapshot = menuItem.Name,
                    SubscriptionPortions = 1,
                    TotalPortions = 1,
                    HasRecipeSnapshot = true,
                    Status = KitchenProductionItemStatus.Ready
                }
            ],
            Materials =
            [
                new KitchenProductionPlanMaterial
                {
                    KitchenIngredient = ingredient,
                    IngredientNameSnapshot = ingredient.Name,
                    UnitSnapshot = ingredient.Unit,
                    RequiredQuantity = requiredQuantity,
                    StockQuantitySnapshot =
                        ingredient.CurrentStockQuantity
                }
            ]
        };
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

    private sealed class NoOpUserNotificationHubContext
        : IHubContext<UserNotificationHub>
    {
        public IHubClients Clients { get; } =
            new NoOpHubClients();

        public IGroupManager Groups { get; } =
            new NoOpGroupManager();
    }

    private sealed class NoOpHubClients : IHubClients
    {
        private static readonly IClientProxy Proxy =
            new NoOpClientProxy();

        public IClientProxy All => Proxy;

        public IClientProxy AllExcept(
            IReadOnlyList<string> excludedConnectionIds)
        {
            return Proxy;
        }

        public IClientProxy Client(string connectionId)
        {
            return Proxy;
        }

        public IClientProxy Clients(
            IReadOnlyList<string> connectionIds)
        {
            return Proxy;
        }

        public IClientProxy Group(string groupName)
        {
            return Proxy;
        }

        public IClientProxy GroupExcept(
            string groupName,
            IReadOnlyList<string> excludedConnectionIds)
        {
            return Proxy;
        }

        public IClientProxy Groups(
            IReadOnlyList<string> groupNames)
        {
            return Proxy;
        }

        public IClientProxy User(string userId)
        {
            return Proxy;
        }

        public IClientProxy Users(IReadOnlyList<string> userIds)
        {
            return Proxy;
        }
    }

    private sealed class NoOpClientProxy : IClientProxy
    {
        public Task SendCoreAsync(
            string method,
            object?[] args,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}
