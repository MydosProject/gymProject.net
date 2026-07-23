using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public class OrdersController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Where(order =>
                order.MemberProfileId != null &&
                order.MemberProfile!.ApplicationUserId == userId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .Select(order => new
            {
                order.OrderNumber,
                order.Type,
                order.Status,
                order.PaymentStatus,
                order.CreatedAtUtc,
                order.DeliveryDate,
                order.DeliveryTimeSlot,
                order.Total,
                Items = order.Items
                    .OrderBy(item => item.Id)
                    .Select(item => new
                    {
                        item.ProductName,
                        item.ItemType,
                        item.UnitPrice,
                        item.Quantity,
                        item.LineTotal
                    })
                    .ToList()
            })
            .ToListAsync();

        var orderViewModels = orders
            .Select(order => new MemberOrderListItemViewModel
            {
                OrderNumber = order.OrderNumber,
                Type = order.Type.ToString(),
                Status = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                CreatedAtUtc = order.CreatedAtUtc,
                CreatedAtLocal = order.CreatedAtUtc.ToLocalTime(),
                DeliveryDate = order.DeliveryDate,
                DeliveryTimeSlot = order.DeliveryTimeSlot,
                Total = order.Total,
                ItemCount = order.Items.Sum(item => item.Quantity),
                Items = order.Items
                    .Select(item => new MemberOrderItemViewModel
                    {
                        ProductName = item.ProductName,
                        ItemType = item.ItemType.ToString(),
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        LineTotal = item.LineTotal
                    })
                    .ToList()
            })
            .ToList();

        return View(new MemberOrdersIndexViewModel
        {
            Orders = orderViewModels
        });
    }
}
