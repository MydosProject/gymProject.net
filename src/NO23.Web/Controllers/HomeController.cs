using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.Models;
using NO23.Web.Services;
using NO23.Web.ViewModels.Home;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Controllers;

public class HomeController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index() => View(new HomeIndexViewModel
    {
        MembershipPackages = await new ServicePackageCatalogService(dbContext).LoadAsync(ServicePackageCategory.Membership),
        KitchenPackages = await dbContext.KitchenSubscriptionPackages.AsNoTracking()
            .Where(x => x.IsActive && (x.Days == 5 || x.Days == 20))
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new KitchenSubscriptionPlanViewModel
            { Plan = x.Plan, Name = x.Name, Description = x.Description, Days = x.Days, UnitPrice = x.UnitPrice })
            .ToListAsync()
    });

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
