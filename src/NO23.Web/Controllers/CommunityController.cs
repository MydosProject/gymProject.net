using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Community;

namespace NO23.Web.Controllers;

public class CommunityController(
    ApplicationDbContext dbContext,
    CommunityChallengeProgressService challengeProgressService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var nowUtc = DateTime.UtcNow;
        var eventRows = await dbContext.CommunityEvents
            .AsNoTracking()
            .Where(item => item.Status != CommunityEventStatus.Cancelled)
            .OrderBy(item => item.StartsAtUtc)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Slug,
                item.Summary,
                Type = item.Type.ToString(),
                item.Status,
                item.StartsAtUtc,
                item.EndsAtUtc,
                item.Location,
                item.Capacity,
                item.IsMembersOnly,
                item.ImageUrl
            })
            .ToListAsync();
        var events = eventRows
            .Where(item => CommunityEventLifecycle.IsPubliclyOpen(
                CommunityEventLifecycle.GetEffectiveStatus(
                    item.Status,
                    item.StartsAtUtc,
                    item.EndsAtUtc,
                    nowUtc)))
            .Select(item => new CommunityEventCardViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Slug = item.Slug,
                Summary = item.Summary,
                Type = item.Type,
                StartsAtUtc = item.StartsAtUtc,
                Location = item.Location,
                Capacity = item.Capacity,
                IsMembersOnly = item.IsMembersOnly,
                ImageUrl = item.ImageUrl
            })
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var challengeRows = await dbContext.CommunityChallenges
            .AsNoTracking()
            .Where(item => item.Status != CommunityChallengeStatus.Cancelled)
            .OrderBy(item => item.StartsOn)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Slug,
                item.Summary,
                item.Goal,
                item.Reward,
                item.StartsOn,
                item.EndsOn,
                item.Status,
                item.ImageUrl
            })
            .ToListAsync();
        var challenges = challengeRows
            .Select(item => new
            {
                item,
                EffectiveStatus = CommunityChallengeLifecycle.GetEffectiveStatus(
                    item.Status,
                    item.StartsOn,
                    item.EndsOn,
                    today)
            })
            .Where(item => CommunityChallengeLifecycle.IsJoinOpen(item.EffectiveStatus))
            .Select(item => new CommunityChallengeCardViewModel
            {
                Id = item.item.Id,
                Title = item.item.Title,
                Slug = item.item.Slug,
                Summary = item.item.Summary,
                Goal = item.item.Goal,
                Reward = item.item.Reward,
                StartsOn = item.item.StartsOn,
                EndsOn = item.item.EndsOn,
                Status = item.EffectiveStatus.ToString(),
                ImageUrl = item.item.ImageUrl
            })
            .ToList();

        return View(new CommunityIndexViewModel
        {
            Events = events,
            Challenges = challenges
        });
    }

    [HttpGet("/Community/Events/{slug}")]
    public async Task<IActionResult> EventDetails(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var normalizedSlug = slug.Trim();
        var eventItem = await dbContext.CommunityEvents
            .AsNoTracking()
            .Where(item =>
                item.Status != CommunityEventStatus.Cancelled &&
                item.Slug == normalizedSlug)
            .Select(item => new CommunityEventDetailViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Slug = item.Slug,
                Summary = item.Summary,
                Description = item.Description,
                Type = item.Type.ToString(),
                StartsAtUtc = item.StartsAtUtc,
                EndsAtUtc = item.EndsAtUtc,
                Location = item.Location,
                Capacity = item.Capacity,
                IsMembersOnly = item.IsMembersOnly,
                ImageUrl = item.ImageUrl
            })
            .SingleOrDefaultAsync();

        if (eventItem is null)
        {
            return NotFound();
        }

        var eventStatus = CommunityEventLifecycle.GetEffectiveStatus(
            CommunityEventStatus.Scheduled,
            eventItem.StartsAtUtc,
            eventItem.EndsAtUtc,
            DateTime.UtcNow);

        if (!CommunityEventLifecycle.IsPubliclyOpen(eventStatus))
        {
            return NotFound();
        }

        return View(eventItem);
    }

    [HttpGet("/Community/Challenges/{slug}")]
    public async Task<IActionResult> ChallengeDetails(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var normalizedSlug = slug.Trim();
        var challenge = await dbContext.CommunityChallenges
            .AsNoTracking()
            .Where(item =>
                item.Status != CommunityChallengeStatus.Cancelled &&
                item.Slug == normalizedSlug)
            .SingleOrDefaultAsync();

        if (challenge is null)
        {
            return NotFound();
        }
        var effectiveStatus = CommunityChallengeLifecycle.GetEffectiveStatus(
            challenge.Status,
            challenge.StartsOn,
            challenge.EndsOn,
            DateOnly.FromDateTime(DateTime.Today));

        if (!CommunityChallengeLifecycle.IsJoinOpen(effectiveStatus))
        {
            return NotFound();
        }

        var range = CommunityChallengeProgressCalculator.GetCalorieRange(
            challenge.TargetDailyCalories,
            challenge.CalorieTolerancePercent);
        var participations = await dbContext.CommunityChallengeParticipations
            .AsNoTracking()
            .Include(item => item.ProgressEntries)
            .Include(item => item.MemberProfile)
            .ThenInclude(member => member.ApplicationUser)
            .Where(item => item.CommunityChallengeId == challenge.Id)
            .ToListAsync();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentMember = string.IsNullOrWhiteSpace(currentUserId)
            ? null
            : await dbContext.MemberProfiles
                .AsNoTracking()
                .Include(member => member.MembershipPackage)
                .FirstOrDefaultAsync(member => member.ApplicationUserId == currentUserId);
        var myParticipation = currentMember is null
            ? null
            : participations.FirstOrDefault(item => item.MemberProfileId == currentMember.Id);
        var myStats = myParticipation is null
            ? null
            : CommunityChallengeProgressCalculator.GetProgressStats(
                challenge.StartsOn,
                challenge.EndsOn,
                challenge.RequiredCompletionPercent,
                myParticipation.ProgressEntries);
        var canJoin =
            currentMember?.MembershipPackage.IncludesCommunityMembership == true &&
            CommunityChallengeLifecycle.IsJoinOpen(effectiveStatus) &&
            myParticipation is null;

        return View(new CommunityChallengeDetailViewModel
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Slug = challenge.Slug,
            Summary = challenge.Summary,
            Description = challenge.Description,
            Goal = challenge.Goal,
            Reward = challenge.Reward,
            TargetDailyCalories = challenge.TargetDailyCalories,
            CalorieTolerancePercent = challenge.CalorieTolerancePercent,
            MinDailyCalories = range.MinCalories,
            MaxDailyCalories = range.MaxCalories,
            RequiredCompletionPercent = challenge.RequiredCompletionPercent,
            StartsOn = challenge.StartsOn,
            EndsOn = challenge.EndsOn,
            Status = effectiveStatus.ToString(),
            ImageUrl = challenge.ImageUrl,
            IsJoined = myParticipation is not null,
            CanJoin = canJoin,
            JoinMessage = GetJoinMessage(User.Identity?.IsAuthenticated == true, currentMember, myParticipation),
            MyParticipationId = myParticipation?.Id,
            MyProgressPercent = myStats?.ProgressPercent ?? 0,
            MyCompliantDays = myStats?.CompliantDays ?? 0,
            MyLoggedDays = myStats?.LoggedDays ?? 0,
            TotalDays = myStats?.TotalDays ??
                Math.Max(1, challenge.EndsOn.DayNumber - challenge.StartsOn.DayNumber + 1),
            Leaderboard = BuildLeaderboard(challenge, participations)
        });
    }

    [HttpPost("/Community/Challenges/{slug}/Join")]
    [Authorize(Roles = ApplicationRoles.Member)]
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

        return RedirectToAction(nameof(ChallengeDetails), new { slug });
    }

    private static IReadOnlyList<ChallengeLeaderboardItemViewModel> BuildLeaderboard(
        CommunityChallenge challenge,
        IReadOnlyList<Domain.Entities.CommunityChallengeParticipation> participations)
    {
        return participations
            .Select(participation =>
            {
                var stats = CommunityChallengeProgressCalculator.GetProgressStats(
                    challenge.StartsOn,
                    challenge.EndsOn,
                    challenge.RequiredCompletionPercent,
                    participation.ProgressEntries);

                return new
                {
                    MemberName = GetMemberName(participation.MemberProfile.ApplicationUser),
                    participation.JoinedAtUtc,
                    participation.Status,
                    Stats = stats
                };
            })
            .OrderByDescending(item => item.Stats.ProgressPercent)
            .ThenByDescending(item => item.Stats.CompliantDays)
            .ThenBy(item => item.JoinedAtUtc)
            .Take(10)
            .Select((item, index) => new ChallengeLeaderboardItemViewModel
            {
                Rank = index + 1,
                MemberName = item.MemberName,
                ProgressPercent = item.Stats.ProgressPercent,
                CompliantDays = item.Stats.CompliantDays,
                LoggedDays = item.Stats.LoggedDays,
                TotalDays = item.Stats.TotalDays,
                IsCompleted = item.Status == CommunityChallengeParticipationStatus.Completed
            })
            .ToList();
    }

    private static string? GetJoinMessage(
        bool isAuthenticated,
        Domain.Entities.MemberProfile? currentMember,
        Domain.Entities.CommunityChallengeParticipation? myParticipation)
    {
        if (myParticipation is not null)
        {
            return "Bu challenge'a katıldın.";
        }

        if (!isAuthenticated)
        {
            return "Katılmak için üye girişi yapmalısın.";
        }

        if (currentMember?.MembershipPackage.IncludesCommunityMembership != true)
        {
            return "Bu challenge'a katılmak için Community üyeliği gerekir.";
        }

        return null;
    }

    private static string GetMemberName(Domain.Entities.ApplicationUser user)
    {
        var firstName = user.FirstName?.Trim();
        var lastName = user.LastName?.Trim();

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            return string.IsNullOrWhiteSpace(lastName)
                ? firstName
                : $"{firstName} {char.ToUpperInvariant(lastName[0])}.";
        }

        return "NO23 Üyesi";
    }
}
