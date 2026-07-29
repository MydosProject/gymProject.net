using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public class CommunityController(
    ApplicationDbContext dbContext,
    CommunityChallengeProgressService challengeProgressService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var profile = await dbContext.MemberProfiles
            .AsNoTracking()
            .Include(member => member.MembershipPackage)
            .FirstOrDefaultAsync(member => member.ApplicationUserId == userId);

        if (profile is null)
        {
            return Challenge();
        }

        var joinedChallengeIds = await dbContext.CommunityChallengeParticipations
            .AsNoTracking()
            .Where(item => item.MemberProfileId == profile.Id)
            .Select(item => item.CommunityChallengeId)
            .ToListAsync();

        var joinedChallengeIdSet = joinedChallengeIds.ToHashSet();
        var challenges = await dbContext.CommunityChallenges
            .AsNoTracking()
            .Where(item =>
                item.Status == CommunityChallengeStatus.Upcoming ||
                item.Status == CommunityChallengeStatus.Active)
            .OrderBy(item => item.Status == CommunityChallengeStatus.Active ? 0 : 1)
            .ThenBy(item => item.StartsOn)
            .ThenBy(item => item.DisplayOrder)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Slug,
                item.Summary,
                item.Goal,
                item.Reward,
                item.Status,
                item.StartsOn,
                item.EndsOn,
                item.TargetDailyCalories,
                item.CalorieTolerancePercent,
                item.RequiredCompletionPercent,
                ParticipantCount = item.Participations.Count(participation =>
                    participation.Status != CommunityChallengeParticipationStatus.Withdrawn)
            })
            .ToListAsync();

        return View(new MemberCommunityIndexViewModel
        {
            HasCommunityMembership = profile.MembershipPackage.IncludesCommunityMembership,
            Challenges = challenges
                .Select(challenge =>
                {
                    var range = CommunityChallengeProgressCalculator.GetCalorieRange(
                        challenge.TargetDailyCalories,
                        challenge.CalorieTolerancePercent);

                    return new MemberCommunityChallengeCardViewModel
                    {
                        Id = challenge.Id,
                        Title = challenge.Title,
                        Slug = challenge.Slug,
                        Summary = challenge.Summary,
                        Goal = challenge.Goal,
                        Reward = challenge.Reward,
                        Status = challenge.Status.ToString(),
                        StartsOn = challenge.StartsOn,
                        EndsOn = challenge.EndsOn,
                        TargetDailyCalories = challenge.TargetDailyCalories,
                        MinDailyCalories = range.MinCalories,
                        MaxDailyCalories = range.MaxCalories,
                        RequiredCompletionPercent = challenge.RequiredCompletionPercent,
                        ParticipantCount = challenge.ParticipantCount,
                        IsJoined = joinedChallengeIdSet.Contains(challenge.Id)
                    };
                })
                .ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinChallenge(string slug)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await challengeProgressService.JoinAsync(userId, slug);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        if (result.Succeeded)
        {
            return RedirectToAction("CalorieTracking", "Goals", new { area = "Member" });
        }

        return RedirectToAction(nameof(Index));
    }
}
