using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NO23.Web.Data.Seed;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
