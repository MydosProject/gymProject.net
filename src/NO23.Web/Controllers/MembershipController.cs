using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NO23.Web.Controllers;

[AllowAnonymous]
public class MembershipController : Controller
{
    [HttpGet]
    public IActionResult Options(string? package = null) =>
        RedirectToAction(nameof(PlansController.Index), "Plans", new { category = "membership" });
}
