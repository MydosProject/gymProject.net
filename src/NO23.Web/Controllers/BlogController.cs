using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Blog;

namespace NO23.Web.Controllers;

public class BlogController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var posts = await dbContext.BlogPosts
            .AsNoTracking()
            .Where(item => item.Status == ContentStatus.Published)
            .OrderByDescending(item => item.PublishedAtUtc ?? item.CreatedAtUtc)
            .Select(item => new BlogPostCardViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Slug = item.Slug,
                Summary = item.Summary,
                Category = item.Category,
                Tags = item.Tags,
                CoverImageUrl = item.CoverImageUrl,
                PublishedAtUtc = item.PublishedAtUtc
            })
            .ToListAsync();

        return View(posts);
    }
}
