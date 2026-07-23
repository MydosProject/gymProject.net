using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NO23.Web.Services;

namespace NO23.Web.ViewComponents;

public class MemberCartSummaryViewComponent(
    MemberCartQueryService cartQueryService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.FindFirstValue(
            ClaimTypes.NameIdentifier);
        var itemCount = string.IsNullOrWhiteSpace(userId)
            ? 0
            : await cartQueryService.GetItemCountAsync(userId);

        return View(itemCount);
    }
}
