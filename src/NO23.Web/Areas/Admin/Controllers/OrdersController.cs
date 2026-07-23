using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class OrdersController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var orders = await dbContext.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.CreatedAtUtc)
            .Select(order => new OrderListItemViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                MemberName = order.MemberProfileId == null
                    ? "Misafir Siparişi"
                    : ((order.MemberProfile!.ApplicationUser.FirstName ?? string.Empty) + " " +
                        (order.MemberProfile.ApplicationUser.LastName ?? string.Empty)).Trim(),
                GuestEmail = order.GuestEmail,
                Type = order.Type.ToString(),
                Status = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                DeliveryDate = order.DeliveryDate,
                DeliveryTimeSlot = order.DeliveryTimeSlot,
                Total = order.Total,
                ItemCount = order.Items.Sum(item => item.Quantity)
            })
            .ToListAsync();

        return View(orders);
    }
}
