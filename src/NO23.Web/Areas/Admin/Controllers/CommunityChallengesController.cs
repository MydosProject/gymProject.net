using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class CommunityChallengesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var challenges = await dbContext.CommunityChallenges
            .AsNoTracking()
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.StartsOn)
            .Select(item => new CommunityChallengeListItemViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Status = item.Status.ToString(),
                StartsOn = item.StartsOn,
                EndsOn = item.EndsOn,
                Goal = item.Goal,
                DisplayOrder = item.DisplayOrder
            })
            .ToListAsync();

        return View(challenges);
    }

    public IActionResult Create()
    {
        return View(new CommunityChallengeFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CommunityChallengeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await SlugExistsAsync(model.Slug, null))
        {
            ModelState.AddModelError(nameof(model.Slug), "This slug is already used.");
            return View(model);
        }

        dbContext.CommunityChallenges.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await dbContext.CommunityChallenges.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        return View(MapToFormModel(item));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CommunityChallengeFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await SlugExistsAsync(model.Slug, id))
        {
            ModelState.AddModelError(nameof(model.Slug), "This slug is already used.");
            return View(model);
        }

        var item = await dbContext.CommunityChallenges.FindAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        ApplyFormModel(item, model);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private Task<bool> SlugExistsAsync(string slug, int? currentId)
    {
        var normalizedSlug = slug.Trim();
        return dbContext.CommunityChallenges.AnyAsync(item =>
            item.Slug == normalizedSlug &&
            (!currentId.HasValue || item.Id != currentId.Value));
    }

    private static CommunityChallenge MapToEntity(CommunityChallengeFormViewModel model)
    {
        var item = new CommunityChallenge();
        ApplyFormModel(item, model);
        return item;
    }

    private static void ApplyFormModel(CommunityChallenge item, CommunityChallengeFormViewModel model)
    {
        item.Title = model.Title.Trim();
        item.Slug = model.Slug.Trim();
        item.Summary = model.Summary.Trim();
        item.Description = model.Description.Trim();
        item.Goal = model.Goal.Trim();
        item.Reward = model.Reward?.Trim();
        item.StartsOn = model.StartsOn;
        item.EndsOn = model.EndsOn;
        item.Status = model.Status;
        item.ImageUrl = model.ImageUrl?.Trim();
        item.DisplayOrder = model.DisplayOrder;
        item.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static CommunityChallengeFormViewModel MapToFormModel(CommunityChallenge item)
    {
        return new CommunityChallengeFormViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Slug = item.Slug,
            Summary = item.Summary,
            Description = item.Description,
            Goal = item.Goal,
            Reward = item.Reward,
            StartsOn = item.StartsOn,
            EndsOn = item.EndsOn,
            Status = item.Status,
            ImageUrl = item.ImageUrl,
            DisplayOrder = item.DisplayOrder
        };
    }
}
