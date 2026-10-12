using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
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
                order.PaymentMethod,
                order.IsManualPayment,
                InstallmentCount = order.PaymentInstallments.Count,
                order.DeliveryMethod,
                DeliveryDate = order.DeliveryDate,
                DeliveryTimeSlot = order.DeliveryTimeSlot,
                Total = order.Total,
                order.DiscountCode,
                order.CampaignDiscountPercent,
                order.DiscountAmount,
                ItemCount = order.Items.Sum(item => item.Quantity),
                Items = order.Items
                    .OrderBy(item => item.Id)
                    .Select(item => new
                    {
                        item.ProductName,
                        item.Quantity,
                        item.RemovedIngredientNames,
                        item.AddedIngredientNames
                    })
                    .ToList()
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
                PaymentMethod = order.PaymentMethod.GetDisplayName(),
                IsManualPayment = order.IsManualPayment,
                AvailableOrderStatuses = order.Type == OrderType.ServicePackagePurchase ? [] : OrderWorkflowService.GetAvailableOrderStatuses(
                    order.Status,
                    order.PaymentStatus),
                AvailablePaymentStatuses = order.IsManualPayment && order.InstallmentCount > 0 && order.PaymentStatus != PaymentStatus.Paid
                    ? [] : OrderWorkflowService.GetAvailablePaymentStatuses(
                    order.Status,
                    order.PaymentStatus),
                DeliveryDate = order.DeliveryDate,
                DeliveryTimeSlot = order.DeliveryTimeSlot,
                DeliveryMethod = order.DeliveryMethod.GetDisplayName(),
                Total = order.Total,
                DiscountCode = order.DiscountCode,
                CampaignDiscountPercent = order.CampaignDiscountPercent,
                DiscountAmount = order.DiscountAmount,
                ItemCount = order.ItemCount,
                Items = order.Items
                    .Select(item => new OrderListItemDetailViewModel
                    {
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        RemovedIngredientNames = item.RemovedIngredientNames,
                        AddedIngredientNames = item.AddedIngredientNames
                    })
                    .ToList()
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

    [HttpGet]
    public async Task<IActionResult> Payment(int id)
    {
        var order = await dbContext.Orders.AsNoTracking()
            .Include(item => item.MemberProfile).ThenInclude(item => item!.ApplicationUser)
            .Include(item => item.ServicePackageVariant).ThenInclude(item => item!.ServicePackage)
            .Include(item => item.PaymentInstallments)
            .Include(item => item.KitchenSubscription).ThenInclude(item => item!.MealPlan)
            .FirstOrDefaultAsync(item => item.Id == id);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordInstallment(int id, int installmentId, DateOnly paidOn, string? reference)
    {
        var error = ModelState.IsValid
            ? await new ManualOrderPaymentService(dbContext).RecordInstallmentAsync(id, installmentId, paidOn,
                reference, User.FindFirstValue(ClaimTypes.NameIdentifier))
            : "Tahsilat bilgilerini kontrol edin.";
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? "Taksit tahsilatı kaydedildi.";
        return RedirectToAction(nameof(Payment), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryActivation(int id)
    {
        var error = await new ManualOrderPaymentService(dbContext).RetryActivationAsync(id);
        TempData[error is null ? "SuccessMessage" : "ErrorMessage"] = error ?? "Kitchen paketi aktifleştirildi.";
        return RedirectToAction(nameof(Payment), new { id });
    }
}
