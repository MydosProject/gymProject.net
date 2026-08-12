using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NO23.Web.Data.Seed;
using NO23.Web.Hubs;
using NO23.Web.Services;

namespace NO23.Web.Controllers;

[Authorize(
    Roles =
        ApplicationRoles.Member +
        "," +
        ApplicationRoles.Trainer +
        "," +
        ApplicationRoles.Admin)]
public class NotificationsController(
    UserNotificationService notificationService,
    IHubContext<UserNotificationHub> hubContext)
    : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(
        [FromForm] int notificationId)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var succeeded =
            await notificationService.MarkAsReadAsync(
                userId,
                notificationId);

        if (!succeeded)
        {
            return NotFound();
        }

        var unreadCount =
            await notificationService.GetUnreadCountAsync(
                userId);

        await PublishStateChangedAsync(
            userId,
            unreadCount);

        return Json(
            new
            {
                unreadCount
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await notificationService.MarkAllAsReadAsync(
            userId);

        await PublishStateChangedAsync(
            userId,
            0);

        return Json(
            new
            {
                unreadCount = 0
            });
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier);
    }

    private async Task PublishStateChangedAsync(
        string userId,
        int unreadCount)
    {
        await hubContext.Clients
            .User(userId)
            .SendAsync(
                "NotificationStateChanged",
                new
                {
                    unreadCount
                });
    }
}
