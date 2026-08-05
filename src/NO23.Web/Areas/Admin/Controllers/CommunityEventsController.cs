using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Extensions;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;
using NO23.Web.Domain.Enums;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class CommunityEventsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var nowUtc = DateTime.UtcNow;
        var eventRows = await dbContext.CommunityEvents
            .AsNoTracking()
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.StartsAtUtc)
            .Select(item => new
            {
                Id = item.Id,
                Title = item.Title,
                Type = item.Type.ToString(),
                item.Status,
                StartsAtUtc = item.StartsAtUtc,
                EndsAtUtc = item.EndsAtUtc,
                Location = item.Location,
                Capacity = item.Capacity,
                DisplayOrder = item.DisplayOrder
            })
            .ToListAsync();

        var events = eventRows
            .Select(item => new CommunityEventListItemViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Type = item.Type,
                Status = CommunityEventLifecycle
                    .GetEffectiveStatus(item.Status, item.StartsAtUtc, item.EndsAtUtc, nowUtc)
                    .GetDisplayName(),
                StartsAtUtc = item.StartsAtUtc,
                Location = item.Location,
                Capacity = item.Capacity,
                DisplayOrder = item.DisplayOrder
            })
            .ToList();

        return View(events);
    }

    public IActionResult Create()
    {
        return View(new CommunityEventFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CommunityEventFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ValidateEventDates(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await SlugExistsAsync(model.Slug, null))
        {
            ModelState.AddModelError(nameof(model.Slug), "Bu URL kısa adı zaten kullanılıyor.");
            return View(model);
        }

        dbContext.CommunityEvents.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await dbContext.CommunityEvents.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        return View(MapToFormModel(item));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CommunityEventFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ValidateEventDates(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await SlugExistsAsync(model.Slug, id))
        {
            ModelState.AddModelError(nameof(model.Slug), "Bu URL kısa adı zaten kullanılıyor.");
            return View(model);
        }

        var item = await dbContext.CommunityEvents.FindAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        ApplyFormModel(item, model);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var item = await dbContext.CommunityEvents.FindAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        var effectiveStatus = CommunityEventLifecycle.GetEffectiveStatus(
            item.Status,
            item.StartsAtUtc,
            item.EndsAtUtc,
            DateTime.UtcNow);

        if (!CommunityEventLifecycle.IsPubliclyOpen(effectiveStatus))
        {
            ModelState.AddModelError(
                string.Empty,
                "Yalnızca planlanmış etkinlikler iptal edilebilir.");

            return View("Edit", MapToFormModel(item));
        }

        item.Status = CommunityEventStatus.Cancelled;
        item.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    private Task<bool> SlugExistsAsync(string slug, int? currentId)
    {
        var normalizedSlug = slug.Trim();
        return dbContext.CommunityEvents.AnyAsync(item =>
            item.Slug == normalizedSlug &&
            (!currentId.HasValue || item.Id != currentId.Value));
    }

    private static CommunityEvent MapToEntity(CommunityEventFormViewModel model)
    {
        var item = new CommunityEvent();
        ApplyFormModel(item, model);
        return item;
    }

    private static void ApplyFormModel(CommunityEvent item, CommunityEventFormViewModel model)
    {
        item.Title = model.Title.Trim();
        item.Slug = model.Slug.Trim();
        item.Summary = model.Summary.Trim();
        item.Description = model.Description.Trim();
        item.Type = model.Type;
        item.StartsAtUtc = DateTime.SpecifyKind(model.StartsAtUtc, DateTimeKind.Utc);
        item.EndsAtUtc = model.EndsAtUtc.HasValue
            ? DateTime.SpecifyKind(model.EndsAtUtc.Value, DateTimeKind.Utc)
            : null;
        item.Status = CommunityEventLifecycle.NormalizeStoredStatus(
            model.Status,
            item.StartsAtUtc,
            item.EndsAtUtc,
            DateTime.UtcNow);
        item.Location = model.Location.Trim();
        item.Capacity = model.Capacity;
        item.IsMembersOnly = model.IsMembersOnly;
        item.ImageUrl = model.ImageUrl?.Trim();
        item.DisplayOrder = model.DisplayOrder;
        item.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static CommunityEventFormViewModel MapToFormModel(CommunityEvent item)
    {
        return new CommunityEventFormViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Slug = item.Slug,
            Summary = item.Summary,
            Description = item.Description,
            Type = item.Type,
            Status = item.Status,
            StartsAtUtc = item.StartsAtUtc,
            EndsAtUtc = item.EndsAtUtc,
            Location = item.Location,
            Capacity = item.Capacity,
            IsMembersOnly = item.IsMembersOnly,
            ImageUrl = item.ImageUrl,
            DisplayOrder = item.DisplayOrder
        };
    }

    private void ValidateEventDates(CommunityEventFormViewModel model)
    {
        if (model.EndsAtUtc.HasValue && model.EndsAtUtc.Value < model.StartsAtUtc)
        {
            ModelState.AddModelError(
                nameof(model.EndsAtUtc),
                "Bitis tarihi baslangic tarihinden once olamaz.");
        }
    }
}
