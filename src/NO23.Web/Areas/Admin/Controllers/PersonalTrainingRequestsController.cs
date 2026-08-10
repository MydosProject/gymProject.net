using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;
using NO23.Web.Extensions;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class PersonalTrainingRequestsController(
    ApplicationDbContext dbContext,
    PersonalTrainingRequestService personalTrainingRequestService,
    UserNotificationRealtimeService notificationService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var rows = await dbContext.PersonalTrainingRequests
            .AsNoTracking()
            .OrderByDescending(request =>
                request.Status == PersonalTrainingRequestStatus.Pending)
            .ThenByDescending(request => request.CreatedAtUtc)
            .Select(request => new
            {
                request.Id,
                MemberName =
                    ((request.MemberProfile.ApplicationUser.FirstName ?? "") + " " +
                     (request.MemberProfile.ApplicationUser.LastName ?? "")).Trim(),
                MemberEmail = request.MemberProfile.ApplicationUser.Email ?? "",
                TrainerName = request.Trainer.FirstName + " " + request.Trainer.LastName,
                request.PreferredDate,
                request.PreferredTimeWindow,
                request.Status,
                request.CreatedAtUtc
            })
            .ToListAsync();

        var requests = rows
            .Select(request => new PersonalTrainingRequestListItemViewModel
            {
                Id = request.Id,
                MemberName = string.IsNullOrWhiteSpace(request.MemberName)
                    ? request.MemberEmail
                    : request.MemberName,
                MemberEmail = request.MemberEmail,
                TrainerName = request.TrainerName,
                PreferredDate = request.PreferredDate,
                PreferredTimeWindow = request.PreferredTimeWindow,
                Status = request.Status.GetDisplayName(),
                IsPending = request.Status == PersonalTrainingRequestStatus.Pending,
                CreatedAtUtc = request.CreatedAtUtc
            })
            .ToList();

        return View(requests);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await BuildFormModelAsync(id);

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PersonalTrainingRequestFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(await RebuildFormModelAsync(id, model));
        }

        var result = await personalTrainingRequestService.UpdateByAdminAsync(
            id,
            model.Status,
            model.ScheduledAtLocal,
            model.AdminNote);

        if (result.Succeeded &&
            model.Status ==
                PersonalTrainingRequestStatus.Completed)
        {
            var requestContext =
                await dbContext
                    .PersonalTrainingRequests
                    .AsNoTracking()
                    .Where(request =>
                        request.Id ==
                            model.Id)
                    .Select(request =>
                        new
                        {
                            MemberUserId =
                                request.MemberProfile
                                    .ApplicationUserId,

                            TrainerName =
                                request.Trainer.FirstName +
                                " " +
                                request.Trainer.LastName,

                            request.Status,

                            request.ScheduledAtUtc,

                            request.TrainerNote
                        })
                    .SingleAsync();

            await notificationService
                .CreateAndPublishAsync(
                    requestContext.MemberUserId,
                    UserNotificationType
                        .PersonalTrainingCompleted,
                    "Birebir antrenmanın tamamlandı",
                    "Birebir antrenman randevun tamamlandı.",
                    "/Member/Reservations#personal-training",
                    model.Id);

            await notificationService
                .PublishPersonalTrainingChangedAsync(
                    requestContext.MemberUserId,
                    model.Id,
                    requestContext.Status,
                    requestContext.ScheduledAtUtc,
                    requestContext.TrainerName,
                    requestContext.TrainerNote);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "İşlem gerçekleştirilemedi.");
            return View(await RebuildFormModelAsync(id, model));
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<PersonalTrainingRequestFormViewModel?> BuildFormModelAsync(int id)
    {
        var request = await dbContext.PersonalTrainingRequests
            .AsNoTracking()
            .Include(item => item.MemberProfile)
            .ThenInclude(member => member.ApplicationUser)
            .Include(item => item.Trainer)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (request is null)
        {
            return null;
        }

        var memberName =
            $"{request.MemberProfile.ApplicationUser.FirstName} {request.MemberProfile.ApplicationUser.LastName}"
                .Trim();
        var memberEmail = request.MemberProfile.ApplicationUser.Email ?? "";

        return new PersonalTrainingRequestFormViewModel
        {
            Id = request.Id,
            MemberName = string.IsNullOrWhiteSpace(memberName) ? memberEmail : memberName,
            MemberEmail = memberEmail,
            TrainerName = request.Trainer.FirstName + " " + request.Trainer.LastName,
            TrainerIsActive = request.Trainer.IsActive,
            PreferredDate = request.PreferredDate,
            PreferredTimeWindow = request.PreferredTimeWindow,
            GoalNote = request.GoalNote,
            CurrentStatus = request.Status,
            CurrentStatusDisplayName = request.Status.GetDisplayName(),
            Status = request.Status,
            ScheduledAtLocal = request.ScheduledAtUtc?.ToLocalTime(),
            TrainerNote = request.TrainerNote,
            AdminNote = request.AdminNote,
            CreatedAtUtc = request.CreatedAtUtc,
            UpdatedAtUtc = request.UpdatedAtUtc,
            CanPlan = request.Trainer.IsActive ||
                request.Status == PersonalTrainingRequestStatus.Scheduled
        };
    }

    private async Task<PersonalTrainingRequestFormViewModel> RebuildFormModelAsync(
        int id,
        PersonalTrainingRequestFormViewModel postedModel)
    {
        var model = await BuildFormModelAsync(id);

        if (model is null)
        {
            return postedModel;
        }

        return new PersonalTrainingRequestFormViewModel
        {
            Id = model.Id,
            MemberName = model.MemberName,
            MemberEmail = model.MemberEmail,
            TrainerName = model.TrainerName,
            TrainerIsActive = model.TrainerIsActive,
            PreferredDate = model.PreferredDate,
            PreferredTimeWindow = model.PreferredTimeWindow,
            GoalNote = model.GoalNote,
            CurrentStatus = model.CurrentStatus,
            CurrentStatusDisplayName = model.CurrentStatusDisplayName,
            Status = postedModel.Status,
            ScheduledAtLocal = postedModel.ScheduledAtLocal,
            TrainerNote = model.TrainerNote,
            AdminNote = postedModel.AdminNote,
            CreatedAtUtc = model.CreatedAtUtc,
            UpdatedAtUtc = model.UpdatedAtUtc,
            CanPlan = model.CanPlan
        };
    }
}
