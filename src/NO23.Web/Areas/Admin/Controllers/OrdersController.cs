using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;
using NO23.Web.Extensions;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class OrdersController(
    ApplicationDbContext dbContext,
    OrderWorkflowService orderWorkflowService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var orderRows = await dbContext.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.CreatedAtUtc)
            .Select(order => new
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                MemberName = order.MemberProfileId == null
                    ? "Misafir Siparişi"
                    : ((order.MemberProfile!.ApplicationUser.FirstName ?? string.Empty) + " " +
                        (order.MemberProfile.ApplicationUser.LastName ?? string.Empty)).Trim(),
                GuestEmail = order.GuestEmail,
                order.Type,
                order.Status,
                order.PaymentStatus,
                DeliveryDate = order.DeliveryDate,
                DeliveryTimeSlot = order.DeliveryTimeSlot,
                Total = order.Total,
                ItemCount = order.Items.Sum(item => item.Quantity)
            })
            .ToListAsync();

        var orders = orderRows
            .Select(order => new OrderListItemViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                MemberName = order.MemberName,
                GuestEmail = order.GuestEmail,
                Type = order.Type.GetDisplayName(),
                Status = order.Status.GetDisplayName(),
                RawStatus = order.Status,
                PaymentStatus = order.PaymentStatus.GetDisplayName(),
                RawPaymentStatus = order.PaymentStatus,
                AvailableOrderStatuses = OrderWorkflowService.GetAvailableOrderStatuses(
                    order.Status,
                    order.PaymentStatus),
                AvailablePaymentStatuses = OrderWorkflowService.GetAvailablePaymentStatuses(
                    order.Status,
                    order.PaymentStatus),
                DeliveryDate = order.DeliveryDate,
                DeliveryTimeSlot = order.DeliveryTimeSlot,
                Total = order.Total,
                ItemCount = order.ItemCount
            })
            .ToList();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, OrderStatus status)
    {
        var result = await orderWorkflowService.UpdateOrderStatusAsync(id, status);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? "Siparis durumu guncellendi."
                : result.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePaymentStatus(int id, PaymentStatus paymentStatus)
    {
        var result = await orderWorkflowService.UpdatePaymentStatusAsync(id, paymentStatus);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? "Odeme durumu guncellendi."
                : result.Message;

        return RedirectToAction(nameof(Index));
    }
}
