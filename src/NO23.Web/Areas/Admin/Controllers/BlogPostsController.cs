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
public class BlogPostsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var posts = await dbContext.BlogPosts
            .AsNoTracking()
            .OrderByDescending(item => item.PublishedAtUtc ?? item.CreatedAtUtc)
            .Select(item => new BlogPostListItemViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Category = item.Category,
                Status = item.Status.ToString(),
                PublishedAtUtc = item.PublishedAtUtc
            })
            .ToListAsync();

        return View(posts);
    }

    public IActionResult Create()
    {
        return View(new BlogPostFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlogPostFormViewModel model)
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

        dbContext.BlogPosts.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await dbContext.BlogPosts.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        return View(MapToFormModel(item));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BlogPostFormViewModel model)
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

        var item = await dbContext.BlogPosts.FindAsync(id);

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
        return dbContext.BlogPosts.AnyAsync(item =>
            item.Slug == normalizedSlug &&
            (!currentId.HasValue || item.Id != currentId.Value));
    }

    private static BlogPost MapToEntity(BlogPostFormViewModel model)
    {
        var item = new BlogPost();
        ApplyFormModel(item, model);
        return item;
    }

    private static void ApplyFormModel(BlogPost item, BlogPostFormViewModel model)
    {
        item.Title = model.Title.Trim();
        item.Slug = model.Slug.Trim();
        item.Summary = model.Summary.Trim();
        item.Content = model.Content.Trim();
        item.Category = model.Category.Trim();
        item.Tags = model.Tags?.Trim();
        item.CoverImageUrl = model.CoverImageUrl?.Trim();
        item.Status = model.Status;
        item.PublishedAtUtc = model.PublishedAtUtc;
        item.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static BlogPostFormViewModel MapToFormModel(BlogPost item)
    {
        return new BlogPostFormViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Slug = item.Slug,
            Summary = item.Summary,
            Content = item.Content,
            Category = item.Category,
            Tags = item.Tags,
            CoverImageUrl = item.CoverImageUrl,
            Status = item.Status,
            PublishedAtUtc = item.PublishedAtUtc
        };
    }
}
