using Microsoft.AspNetCore.Identity;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Services;

public class AdminStockNotificationService(
    UserManager<ApplicationUser> userManager,
    UserNotificationRealtimeService notificationRealtimeService)
{
    public async Task PublishKitchenStockChangedAsync(
        int ingredientId,
        string ingredientName,
        decimal previousStockQuantity,
        decimal currentStockQuantity,
        decimal minimumStockQuantity,
        string unitText)
    {
        var transition = DetermineTransition(
            previousStockQuantity,
            currentStockQuantity,
            minimumStockQuantity);

        if (transition == StockTransition.None)
        {
            return;
        }

        var type =
            transition == StockTransition.Out
                ? UserNotificationType.KitchenStockOut
                : UserNotificationType.KitchenStockCritical;

        var title =
            transition == StockTransition.Out
                ? "Kitchen stoğu tükendi"
                : "Kitchen stoğu kritik seviyede";

        var message =
            transition == StockTransition.Out
                ? $"{ingredientName} stoğu tükendi."
                : $"{ingredientName} kritik stok seviyesine düştü. " +
                  $"Kalan: {FormatQuantity(currentStockQuantity)} {unitText}, " +
                  $"minimum: {FormatQuantity(minimumStockQuantity)} {unitText}.";

        await PublishToAdminsAsync(
            type,
            title,
            message,
            "/Admin/KitchenStock",
            ingredientId);
    }

    public async Task PublishShopStockChangedAsync(
        int productId,
        string productName,
        int previousStockQuantity,
        int currentStockQuantity,
        int minimumStockQuantity)
    {
        var transition = DetermineTransition(
            previousStockQuantity,
            currentStockQuantity,
            minimumStockQuantity);

        if (transition == StockTransition.None)
        {
            return;
        }

        var type =
            transition == StockTransition.Out
                ? UserNotificationType.ShopStockOut
                : UserNotificationType.ShopStockCritical;

        var title =
            transition == StockTransition.Out
                ? "Shop ürünü tükendi"
                : "Shop stoğu kritik seviyede";

        var message =
            transition == StockTransition.Out
                ? $"{productName} stoğu tükendi."
                : $"{productName} kritik stok seviyesine düştü. " +
                  $"Kalan stok: {currentStockQuantity}, " +
                  $"kritik eşik: {minimumStockQuantity}.";

        await PublishToAdminsAsync(
            type,
            title,
            message,
            "/Admin/ShopProducts",
            productId);
    }

    private async Task PublishToAdminsAsync(
        UserNotificationType type,
        string title,
        string message,
        string url,
        int relatedEntityId)
    {
        var adminUsers =
            await userManager.GetUsersInRoleAsync(
                ApplicationRoles.Admin);

        foreach (var adminUser in adminUsers)
        {
            await notificationRealtimeService
                .CreateAndPublishAsync(
                    adminUser.Id,
                    type,
                    title,
                    message,
                    url,
                    relatedEntityId);
        }
    }

    private static StockTransition DetermineTransition(
        decimal previousStockQuantity,
        decimal currentStockQuantity,
        decimal minimumStockQuantity)
    {
        var previousState =
            DetermineState(
                previousStockQuantity,
                minimumStockQuantity);

        var currentState =
            DetermineState(
                currentStockQuantity,
                minimumStockQuantity);

        if (currentState == StockState.Out &&
            previousState != StockState.Out)
        {
            return StockTransition.Out;
        }

        if (currentState == StockState.Critical &&
            previousState == StockState.Normal)
        {
            return StockTransition.Critical;
        }

        return StockTransition.None;
    }

    private static StockState DetermineState(
        decimal stockQuantity,
        decimal minimumStockQuantity)
    {
        if (stockQuantity <= 0)
        {
            return StockState.Out;
        }

        if (stockQuantity <= minimumStockQuantity)
        {
            return StockState.Critical;
        }

        return StockState.Normal;
    }

    private static string FormatQuantity(
        decimal quantity)
    {
        return quantity.ToString("0.##");
    }

    private enum StockState
    {
        Normal,
        Critical,
        Out
    }

    private enum StockTransition
    {
        None,
        Critical,
        Out
    }
}