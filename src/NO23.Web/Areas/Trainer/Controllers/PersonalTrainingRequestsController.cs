using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;
using NO23.Web.Extensions;
using NO23.Web.Services;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Web.Areas.Trainer.Controllers;

[Area("Trainer")]
[Authorize(Roles = ApplicationRoles.Trainer)]
public class PersonalTrainingRequestsController(
    ApplicationDbContext dbContext,
    PersonalTrainingRequestService personalTrainingRequestService)
    : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var trainerId = await GetTrainerIdAsync(userId);

        if (trainerId is null)
        {
            return Forbid();
        }

        var rows =
            await dbContext.PersonalTrainingRequests
                .AsNoTracking()
                .Where(request =>
                    request.TrainerId == trainerId.Value)
                .OrderByDescending(request =>
                    request.Status ==
                    PersonalTrainingRequestStatus.Pending)
                .ThenByDescending(request =>
                    request.CreatedAtUtc)
                .Select(request => new
                {
                    request.Id,

                    MemberFirstName =
                        request.MemberProfile.ApplicationUser
                            .FirstName,

                    MemberLastName =
                        request.MemberProfile.ApplicationUser
                            .LastName,

                    MemberEmail =
                        request.MemberProfile.ApplicationUser
                            .Email,

                    request.PreferredDate,
                    request.PreferredTimeWindow,
                    request.GoalNote,
                    request.Status,
                    request.ScheduledAtUtc,
                    request.CreatedAtUtc
                })
                .ToListAsync();

        var model = rows
            .Select(request =>
            {
                var memberName =
                    $"{request.MemberFirstName} {request.MemberLastName}"
                        .Trim();

                var memberEmail =
                    request.MemberEmail ?? string.Empty;

                return new
                    TrainerPersonalTrainingRequestListItemViewModel
                    {
                        Id = request.Id,

                        MemberName =
                            string.IsNullOrWhiteSpace(memberName)
                                ? memberEmail
                                : memberName,

                        MemberEmail = memberEmail,

                        PreferredDate =
                            request.PreferredDate,

                        PreferredTimeWindow =
                            request.PreferredTimeWindow,

                        GoalNote =
                            request.GoalNote,

                        Status =
                            request.Status,

                        StatusDisplayName =
                            request.Status.GetDisplayName(),

                        ScheduledAtUtc =
                            request.ScheduledAtUtc,

                        CreatedAtUtc =
                            request.CreatedAtUtc
                    };
            })
            .ToList();

        return View(model);
    }

    public async Task<IActionResult> Manage(int id)
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var model =
            await BuildManageModelAsync(id, userId);

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Schedule(
        int id,
        TrainerPersonalTrainingRequestManageViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return await RebuildManageViewAsync(
                id,
                userId,
                model);
        }

        var result =
            await personalTrainingRequestService
                .UpdateByTrainerAsync(
                    userId,
                    id,
                    PersonalTrainingRequestStatus.Scheduled,
                    model.ScheduledAtLocal,
                    model.TrainerNote);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(
                string.Empty,
                result.ErrorMessage
                ?? "Randevu planlanamadı.");

            return await RebuildManageViewAsync(
                id,
                userId,
                model);
        }

        TempData["TrainerStatusMessage"] =
            "Birebir randevu başarıyla planlandı.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        int id,
        TrainerPersonalTrainingRequestManageViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return await RebuildManageViewAsync(
                id,
                userId,
                model);
        }

        var result =
            await personalTrainingRequestService
                .UpdateByTrainerAsync(
                    userId,
                    id,
                    PersonalTrainingRequestStatus.Rejected,
                    null,
                    model.TrainerNote);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(
                string.Empty,
                result.ErrorMessage
                ?? "Talep reddedilemedi.");

            return await RebuildManageViewAsync(
                id,
                userId,
                model);
        }

        TempData["TrainerStatusMessage"] =
            "Birebir talep reddedildi.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> GetTrainerIdAsync(
        string userId)
    {
        return await dbContext.Trainers
            .AsNoTracking()
            .Where(trainer =>
                trainer.ApplicationUserId == userId)
            .Select(trainer => (int?)trainer.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<
        TrainerPersonalTrainingRequestManageViewModel?>
        BuildManageModelAsync(
            int requestId,
            string userId)
    {
        var request =
            await dbContext.PersonalTrainingRequests
                .AsNoTracking()
                .Where(item =>
                    item.Id == requestId &&
                    item.Trainer.ApplicationUserId ==
                    userId)
                .Select(item => new
                {
                    item.Id,

                    MemberFirstName =
                        item.MemberProfile.ApplicationUser
                            .FirstName,

                    MemberLastName =
                        item.MemberProfile.ApplicationUser
                            .LastName,

                    MemberEmail =
                        item.MemberProfile.ApplicationUser
                            .Email,

                    item.PreferredDate,
                    item.PreferredTimeWindow,
                    item.GoalNote,
                    item.Status,
                    item.ScheduledAtUtc,
                    item.TrainerNote,
                    item.CreatedAtUtc
                })
                .FirstOrDefaultAsync();

        if (request is null)
        {
            return null;
        }

        var memberName =
            $"{request.MemberFirstName} {request.MemberLastName}"
                .Trim();

        var memberEmail =
            request.MemberEmail ?? string.Empty;

        return new
            TrainerPersonalTrainingRequestManageViewModel
            {
                Id = request.Id,

                MemberName =
                    string.IsNullOrWhiteSpace(memberName)
                        ? memberEmail
                        : memberName,

                MemberEmail =
                    memberEmail,

                PreferredDate =
                    request.PreferredDate,

                PreferredTimeWindow =
                    request.PreferredTimeWindow,

                GoalNote =
                    request.GoalNote,

                CurrentStatus =
                    request.Status,

                CurrentStatusDisplayName =
                    request.Status.GetDisplayName(),

                ScheduledAtLocal =
                    request.ScheduledAtUtc?.ToLocalTime(),

                TrainerNote =
                    request.TrainerNote,

                CreatedAtUtc =
                    request.CreatedAtUtc
            };
    }

    private async Task<IActionResult>
        RebuildManageViewAsync(
            int id,
            string userId,
            TrainerPersonalTrainingRequestManageViewModel postedModel)
    {
        var model =
            await BuildManageModelAsync(id, userId);

        if (model is null)
        {
            return NotFound();
        }

        model.ScheduledAtLocal =
            postedModel.ScheduledAtLocal;

        model.TrainerNote =
            postedModel.TrainerNote;

        return View("Manage", model);
    }
}