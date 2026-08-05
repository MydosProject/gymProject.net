using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class CommunityChallengesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var challengeRows = await dbContext.CommunityChallenges
            .AsNoTracking()
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.StartsOn)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Status,
                item.StartsOn,
                item.EndsOn,
                item.Goal,
                item.TargetDailyCalories,
                item.CalorieTolerancePercent,
                item.RequiredCompletionPercent,
                ParticipantCount = item.Participations.Count,
                item.DisplayOrder
            })
            .ToListAsync();
        var challenges = challengeRows
            .Select(item => new CommunityChallengeListItemViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Status = CommunityChallengeLifecycle.GetEffectiveStatus(
                        item.Status,
                        item.StartsOn,
                        item.EndsOn,
                        today)
                    .ToString(),
                StartsOn = item.StartsOn,
                EndsOn = item.EndsOn,
                Goal = item.Goal,
                TargetDailyCalories = item.TargetDailyCalories,
                CalorieTolerancePercent = item.CalorieTolerancePercent,
                RequiredCompletionPercent = item.RequiredCompletionPercent,
                ParticipantCount = item.ParticipantCount,
                DisplayOrder = item.DisplayOrder
            })
            .ToList();

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

        ValidateChallengeDates(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await SlugExistsAsync(model.Slug, null))
        {
            ModelState.AddModelError(nameof(model.Slug), "Bu URL kısa adı zaten kullanılıyor.");
            return View(model);
        }

        dbContext.CommunityChallenges.Add(MapToEntity(model));
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var challenge = await dbContext.CommunityChallenges
            .AsNoTracking()
            .Include(item => item.Participations)
            .ThenInclude(participation => participation.ProgressEntries)
            .Include(item => item.Participations)
            .ThenInclude(participation => participation.MemberProfile)
            .ThenInclude(member => member.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (challenge is null)
        {
            return NotFound();
        }

        var range = CommunityChallengeProgressCalculator.GetCalorieRange(
            challenge.TargetDailyCalories,
            challenge.CalorieTolerancePercent);
        var participants = challenge.Participations
            .Select(participation =>
            {
                var stats = CommunityChallengeProgressCalculator.GetProgressStats(
                    challenge.StartsOn,
                    challenge.EndsOn,
                    challenge.RequiredCompletionPercent,
                    participation.ProgressEntries);

                return new CommunityChallengeParticipantViewModel
                {
                    MemberName = GetMemberName(participation.MemberProfile.ApplicationUser),
                    MemberEmail = participation.MemberProfile.ApplicationUser.Email ?? string.Empty,
                    Status = GetParticipationStatusName(participation.Status),
                    JoinedAtUtc = participation.JoinedAtUtc,
                    LoggedDays = stats.LoggedDays,
                    CompliantDays = stats.CompliantDays,
                    TotalDays = stats.TotalDays,
                    ProgressPercent = stats.ProgressPercent
                };
            })
            .OrderByDescending(item => item.ProgressPercent)
            .ThenByDescending(item => item.CompliantDays)
            .ThenBy(item => item.MemberName)
            .ToList();
        var recentLogs = challenge.Participations
            .SelectMany(participation => participation.ProgressEntries
                .OrderByDescending(entry => entry.EntryDate)
                .Take(5)
                .Select(entry => new CommunityChallengeLogViewModel
                {
                    MemberName = GetMemberName(participation.MemberProfile.ApplicationUser),
                    EntryDate = entry.EntryDate,
                    CaloriesConsumed = entry.CaloriesConsumed,
                    MinCalories = entry.MinCaloriesSnapshot,
                    MaxCalories = entry.MaxCaloriesSnapshot,
                    IsCompliant = entry.IsCompliant
                }))
            .OrderByDescending(item => item.EntryDate)
            .ThenBy(item => item.MemberName)
            .Take(20)
            .ToList();

        return View(new CommunityChallengeDetailsViewModel
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Status = CommunityChallengeLifecycle.GetEffectiveStatus(
                    challenge.Status,
                    challenge.StartsOn,
                    challenge.EndsOn,
                    DateOnly.FromDateTime(DateTime.Today))
                .ToString(),
            StartsOn = challenge.StartsOn,
            EndsOn = challenge.EndsOn,
            Goal = challenge.Goal,
            TargetDailyCalories = challenge.TargetDailyCalories,
            CalorieTolerancePercent = challenge.CalorieTolerancePercent,
            MinDailyCalories = range.MinCalories,
            MaxDailyCalories = range.MaxCalories,
            RequiredCompletionPercent = challenge.RequiredCompletionPercent,
            Participants = participants,
            RecentLogs = recentLogs
        });
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

        ValidateChallengeDates(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await SlugExistsAsync(model.Slug, id))
        {
            ModelState.AddModelError(nameof(model.Slug), "Bu URL kısa adı zaten kullanılıyor.");
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var item = await dbContext.CommunityChallenges.FindAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        var effectiveStatus = CommunityChallengeLifecycle.GetEffectiveStatus(
            item.Status,
            item.StartsOn,
            item.EndsOn,
            DateOnly.FromDateTime(DateTime.Today));

        if (!CommunityChallengeLifecycle.IsJoinOpen(effectiveStatus))
        {
            return RedirectToAction(nameof(Index));
        }

        item.Status = CommunityChallengeStatus.Cancelled;
        item.UpdatedAtUtc = DateTime.UtcNow;

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
        item.TargetDailyCalories = model.TargetDailyCalories;
        item.CalorieTolerancePercent = model.CalorieTolerancePercent;
        item.RequiredCompletionPercent = model.RequiredCompletionPercent;
        item.StartsOn = model.StartsOn;
        item.EndsOn = model.EndsOn;
        item.Status = CommunityChallengeLifecycle.NormalizeStoredStatus(
            model.Status,
            model.StartsOn,
            model.EndsOn,
            DateOnly.FromDateTime(DateTime.Today));
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
            TargetDailyCalories = item.TargetDailyCalories,
            CalorieTolerancePercent = item.CalorieTolerancePercent,
            RequiredCompletionPercent = item.RequiredCompletionPercent,
            StartsOn = item.StartsOn,
            EndsOn = item.EndsOn,
            Status = item.Status,
            ImageUrl = item.ImageUrl,
            DisplayOrder = item.DisplayOrder
        };
    }

    private void ValidateChallengeDates(CommunityChallengeFormViewModel model)
    {
        if (model.EndsOn < model.StartsOn)
        {
            ModelState.AddModelError(
                nameof(model.EndsOn),
                "Bitiş tarihi başlangıç tarihinden önce olamaz.");
        }
    }

    private static string GetMemberName(ApplicationUser user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return user.UserName ?? user.Email ?? "NO23 Üyesi";
    }

    private static string GetParticipationStatusName(CommunityChallengeParticipationStatus status)
    {
        return status switch
        {
            CommunityChallengeParticipationStatus.Active => "Devam ediyor",
            CommunityChallengeParticipationStatus.Completed => "Tamamlandı",
            CommunityChallengeParticipationStatus.Withdrawn => "Ayrıldı",
            _ => status.ToString()
        };
    }
}
