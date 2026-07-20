using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.SuccessStories;

namespace NO23.Web.Controllers;

public class SuccessStoriesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var stories = await dbContext.SuccessStories
            .AsNoTracking()
            .Where(item => item.Status == ContentStatus.Published)
            .OrderByDescending(item => item.PublishedAtUtc ?? item.CreatedAtUtc)
            .Select(item => new SuccessStoryCardViewModel
            {
                Id = item.Id,
                MemberName = item.MemberName,
                Title = item.Title,
                Slug = item.Slug,
                Summary = item.Summary,
                AchievementMetric = item.AchievementMetric,
                BeforeImageUrl = item.BeforeImageUrl,
                AfterImageUrl = item.AfterImageUrl,
                VideoUrl = item.VideoUrl,
                PublishedAtUtc = item.PublishedAtUtc
            })
            .ToListAsync();

        return View(stories);
    }
}
