using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Extensions;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class SuccessStoriesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var storyRows = await dbContext.SuccessStories
            .AsNoTracking()
            .OrderByDescending(item => item.PublishedAtUtc ?? item.CreatedAtUtc)
            .Select(item => new
            {
                Id = item.Id,
                MemberName = item.MemberName,
                Title = item.Title,
                item.Status,
                AchievementMetric = item.AchievementMetric,
                PublishedAtUtc = item.PublishedAtUtc
            })
            .ToListAsync();

        var stories = storyRows
            .Select(item => new SuccessStoryListItemViewModel
            {
                Id = item.Id,
                MemberName = item.MemberName,
                Title = item.Title,
                Status = item.Status.GetDisplayName(),
                AchievementMetric = item.AchievementMetric,
                PublishedAtUtc = item.PublishedAtUtc
            })
            .ToList();

        return View(stories);
    }

    public IActionResult Create()
    {
        return View(new SuccessStoryFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SuccessStoryFormViewModel model)
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

        dbContext.SuccessStories.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await dbContext.SuccessStories.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        return View(MapToFormModel(item));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SuccessStoryFormViewModel model)
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

        var item = await dbContext.SuccessStories.FindAsync(id);

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
        return dbContext.SuccessStories.AnyAsync(item =>
            item.Slug == normalizedSlug &&
            (!currentId.HasValue || item.Id != currentId.Value));
    }

    private static SuccessStory MapToEntity(SuccessStoryFormViewModel model)
    {
        var item = new SuccessStory();
        ApplyFormModel(item, model);
        return item;
    }

    private static void ApplyFormModel(SuccessStory item, SuccessStoryFormViewModel model)
    {
        item.MemberName = model.MemberName.Trim();
        item.Title = model.Title.Trim();
        item.Slug = model.Slug.Trim();
        item.Summary = model.Summary.Trim();
        item.Story = model.Story.Trim();
        item.AchievementMetric = model.AchievementMetric?.Trim();
        item.BeforeImageUrl = model.BeforeImageUrl?.Trim();
        item.AfterImageUrl = model.AfterImageUrl?.Trim();
        item.VideoUrl = model.VideoUrl?.Trim();
        item.Status = model.Status;
        item.PublishedAtUtc = model.PublishedAtUtc.HasValue
            ? DateTime.SpecifyKind(model.PublishedAtUtc.Value, DateTimeKind.Utc)
            : null;
        item.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static SuccessStoryFormViewModel MapToFormModel(SuccessStory item)
    {
        return new SuccessStoryFormViewModel
        {
            Id = item.Id,
            MemberName = item.MemberName,
            Title = item.Title,
            Slug = item.Slug,
            Summary = item.Summary,
            Story = item.Story,
            AchievementMetric = item.AchievementMetric,
            BeforeImageUrl = item.BeforeImageUrl,
            AfterImageUrl = item.AfterImageUrl,
            VideoUrl = item.VideoUrl,
            Status = item.Status,
            PublishedAtUtc = item.PublishedAtUtc
        };
    }
}