using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Membership;

namespace NO23.Web.Controllers;

[AllowAnonymous]
public class MembershipController(ApplicationDbContext dbContext) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Options(string package)
    {
        if (!Enum.TryParse<MembershipPackageCode>(package, true, out var packageCode)) return NotFound();
        var packageData = await dbContext.MembershipPackages.AsNoTracking()
            .Where(x => x.Code == packageCode && x.IsActive)
            .Select(x => new
            {
                x.Code, x.Name, x.Audience,
                Options = x.Options.Where(option => option.IsActive)
                    .OrderBy(option => option.DisplayOrder).ThenBy(option => option.Name)
                    .Select(option => new MembershipServiceOptionViewModel
                    {
                        Id = option.Id, Name = option.Name, Description = option.Description,
                        DurationDays = option.DurationDays,
                        PersonalTrainingSessionCount = option.PersonalTrainingSessionCount,
                        GroupClassCreditCount = option.GroupClassCreditCount,
                        IncludesGymAccess = option.IncludesGymAccess,
                        DisplayOrder = option.DisplayOrder
                    }).ToList()
            }).FirstOrDefaultAsync();
        if (packageData is null) return NotFound();
        var model = new MembershipOptionsViewModel
        {
            PackageCode = packageData.Code.ToString().ToUpperInvariant(),
            PackageName = packageData.Name, PackageAudience = packageData.Audience,
            Options = packageData.Options
        };
        if (model.Options.Count == 0)
            return RedirectToPage("/Account/Register", new { area = "Identity", package = model.PackageCode });
        return View(model);
    }
}
