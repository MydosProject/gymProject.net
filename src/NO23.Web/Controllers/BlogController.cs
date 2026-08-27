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
                Content = item.Content,
                Category = item.Category,
                Tags = item.Tags,
                CoverImageUrl = item.CoverImageUrl,
                PublishedAtUtc = item.PublishedAtUtc,
                CreatedAtUtc = item.CreatedAtUtc
            })
            .ToListAsync();

        return View(posts);
    }

    [HttpGet("/Blog/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var normalizedSlug = slug.Trim();
        var post = await dbContext.BlogPosts
            .AsNoTracking()
            .Where(item =>
                item.Status == ContentStatus.Published &&
                item.Slug == normalizedSlug)
            .Select(item => new BlogPostDetailViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Slug = item.Slug,
                Summary = item.Summary,
                Content = item.Content,
                Category = item.Category,
                Tags = item.Tags,
                CoverImageUrl = item.CoverImageUrl,
                PublishedAtUtc = item.PublishedAtUtc,
                CreatedAtUtc = item.CreatedAtUtc
            })
            .SingleOrDefaultAsync();

        if (post is null)
        {
            return NotFound();
        }

        return View(post);
    }
}
