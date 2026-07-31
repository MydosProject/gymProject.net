using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Extensions;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class OrdersController(ApplicationDbContext dbContext) : Controller
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
                PaymentStatus = order.PaymentStatus.GetDisplayName(),
                DeliveryDate = order.DeliveryDate,
                DeliveryTimeSlot = order.DeliveryTimeSlot,
                Total = order.Total,
                ItemCount = order.ItemCount
            })
            .ToList();

        return View(orders);
    }
}
