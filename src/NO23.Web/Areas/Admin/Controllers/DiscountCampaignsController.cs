using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class DiscountCampaignsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var campaigns = await dbContext.DiscountCampaigns
            .AsNoTracking()
            .OrderByDescending(campaign => campaign.IsActive)
            .ThenByDescending(campaign => campaign.CreatedAtUtc)
            .Select(campaign => new DiscountCampaignListItemViewModel
            {
                Id = campaign.Id,
                Code = campaign.Code,
                Name = campaign.Name,
                DiscountPercent = campaign.DiscountPercent,
                MinimumSubtotal = campaign.MinimumSubtotal,
                StartsAtUtc = campaign.StartsAtUtc,
                EndsAtUtc = campaign.EndsAtUtc,
                UsageLimit = campaign.UsageLimit,
                UsedCount = campaign.UsedCount,
                IsActive = campaign.IsActive
            })
            .ToListAsync();

        return View(campaigns);
    }

    public IActionResult Create() => View(new DiscountCampaignFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DiscountCampaignFormViewModel model)
    {
        await ValidateCodeAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var campaign = new DiscountCampaign();
        ApplyForm(campaign, model);
        dbContext.DiscountCampaigns.Add(campaign);
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "İndirim kampanyası oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var campaign = await dbContext.DiscountCampaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (campaign is null)
        {
            return NotFound();
        }

        return View(new DiscountCampaignFormViewModel
        {
            Id = campaign.Id,
            Code = campaign.Code,
            Name = campaign.Name,
            DiscountPercent = campaign.DiscountPercent,
            MinimumSubtotal = campaign.MinimumSubtotal,
            StartsAt = campaign.StartsAtUtc.HasValue
                ? ClubTime.ToLocal(campaign.StartsAtUtc.Value)
                : null,
            EndsAt = campaign.EndsAtUtc.HasValue
                ? ClubTime.ToLocal(campaign.EndsAtUtc.Value)
                : null,
            UsageLimit = campaign.UsageLimit,
            IsActive = campaign.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DiscountCampaignFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        await ValidateCodeAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var campaign = await dbContext.DiscountCampaigns.FindAsync(id);

        if (campaign is null)
        {
            return NotFound();
        }

        ApplyForm(campaign, model);
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "İndirim kampanyası güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateCodeAsync(DiscountCampaignFormViewModel model)
    {
        var code = model.Code?.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(code) &&
            await dbContext.DiscountCampaigns.AnyAsync(item =>
                item.Code == code && item.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.Code), "Bu kampanya kodu zaten kullanılıyor.");
        }
    }

    private static void ApplyForm(
        DiscountCampaign campaign,
        DiscountCampaignFormViewModel model)
    {
        campaign.Code = model.Code.Trim().ToUpperInvariant();
        campaign.Name = model.Name.Trim();
        campaign.DiscountPercent = model.DiscountPercent;
        campaign.MinimumSubtotal = model.MinimumSubtotal;
        campaign.StartsAtUtc = ToUtc(model.StartsAt);
        campaign.EndsAtUtc = ToUtc(model.EndsAt);
        campaign.UsageLimit = model.UsageLimit;
        campaign.IsActive = model.IsActive;
        campaign.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : ClubTime.ToUtc(value.Value);
    }
}
