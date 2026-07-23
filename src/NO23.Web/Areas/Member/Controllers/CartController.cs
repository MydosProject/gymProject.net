using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NO23.Web.Data.Seed;
using NO23.Web.Services;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public class CartController(MemberCartQueryService cartQueryService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Panel()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var model = await cartQueryService.BuildPanelAsync(userId);

        return PartialView(
            "~/Areas/Member/Views/Shared/_MemberCartDrawerContent.cshtml",
            model);
    }
}
