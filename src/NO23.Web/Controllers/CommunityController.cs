using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Enums;
using NO23.Web.ViewModels.Community;

namespace NO23.Web.Controllers;

public class CommunityController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var events = await dbContext.CommunityEvents
            .AsNoTracking()
            .Where(item => item.Status == CommunityEventStatus.Scheduled)
            .OrderBy(item => item.StartsAtUtc)
            .Select(item => new CommunityEventCardViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Slug = item.Slug,
                Summary = item.Summary,
                Type = item.Type.ToString(),
                StartsAtUtc = item.StartsAtUtc,
                Location = item.Location,
                Capacity = item.Capacity,
                IsMembersOnly = item.IsMembersOnly,
                ImageUrl = item.ImageUrl
            })
            .ToListAsync();

        var challenges = await dbContext.CommunityChallenges
            .AsNoTracking()
            .Where(item =>
                item.Status == CommunityChallengeStatus.Upcoming ||
                item.Status == CommunityChallengeStatus.Active)
            .OrderBy(item => item.StartsOn)
            .Select(item => new CommunityChallengeCardViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Slug = item.Slug,
                Summary = item.Summary,
                Goal = item.Goal,
                Reward = item.Reward,
                StartsOn = item.StartsOn,
                EndsOn = item.EndsOn,
                Status = item.Status.ToString(),
                ImageUrl = item.ImageUrl
            })
            .ToListAsync();

        return View(new CommunityIndexViewModel
        {
            Events = events,
            Challenges = challenges
        });
    }
}
