using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;
using NO23.Web.ViewModels;
using NO23.Web.Extensions;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = ApplicationRoles.Admin)]
public class AppointmentRequestsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(int page = 1, string? query = null, string? state = null)
    {
        query = query?.Trim();
        if (!ModelState.IsValid || query?.Length > 80) return BadRequest();
        var requests = dbContext.AppointmentRequests.AsNoTracking().Include(x => x.ScheduledTrainer).AsQueryable();
        if (!string.IsNullOrEmpty(state))
        {
            if (!Enum.TryParse<ServicePackageApplicationStatus>(state, true, out var status) || !Enum.IsDefined(status)) return BadRequest();
            requests = requests.Where(x => x.Status == status);
            state = status.ToString();
        }
        if (!string.IsNullOrEmpty(query))
        {
            var term = query.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
            int.TryParse(query.TrimStart('#'), out var id);
            requests = requests.Where(x => x.Id == id || x.FullName.Replace("I", "ı").Replace("İ", "i").ToLower().Contains(term) || x.PhoneNumber.Contains(query));
        }
        var navigation = new ListNavigationViewModel(await requests.CountAsync(), page, query, state,
            Enum.GetValues<ServicePackageApplicationStatus>().Select(x => new SelectListItem(x.GetDisplayName(), x.ToString())).ToArray());
        ViewData["ListNavigation"] = navigation;
        return View(await requests.OrderBy(x => x.Status != ServicePackageApplicationStatus.Pending)
            .ThenByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Skip(navigation.Skip).Take(ListNavigationViewModel.PageSize).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Schedule(int id)
    {
        var model = await PopulateAsync(new AppointmentScheduleViewModel { Id = id });
        if (model is null) return NotFound();
        model.TrainerId = model.Request.ScheduledTrainerId ?? 0;
        model.StartsAt = model.Request.ScheduledAtUtc.HasValue ? ClubTime.ToLocal(model.Request.ScheduledAtUtc.Value)
            : model.Request.PreferredDate.ToDateTime(model.Request.PreferredTime);
        model.DurationMinutes = model.Request.DurationMinutes;
        model.ExpectedVersion = model.Request.Version;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Schedule(int id, AppointmentScheduleViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            var result = await new AppointmentSchedulingService(dbContext).ScheduleAsync(model, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Index", "Calendar", new { week = model.StartsAt.ToString("yyyy-MM-dd"), trainerId = model.TrainerId });
            }
            ModelState.AddModelError(string.Empty, result.Message);
        }
        return await PopulateAsync(model) is { } page ? View(page) : NotFound();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int id, Guid? expectedVersion, AppointmentScheduleStatus status, string? note)
    {
        if (!ModelState.IsValid || expectedVersion is null) return BadRequest();
        var result = await new AppointmentSchedulingService(dbContext).FinishAsync(id, expectedVersion.Value, status,
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", note);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Schedule), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, ServicePackageApplicationStatus status, int page = 1, string? query = null, string? state = null)
    {
        if (!Enum.IsDefined(status)) return BadRequest();
        if (!await dbContext.AppointmentRequests.AnyAsync(x => x.Id == id)) return NotFound();
        var result = await new AppointmentSchedulingService(dbContext).UpdateRequestStatusAsync(id, status,
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { page, query, state });
    }

    private async Task<AppointmentScheduleViewModel?> PopulateAsync(AppointmentScheduleViewModel model)
    {
        var request = await dbContext.AppointmentRequests.AsNoTracking().Include(x => x.ScheduledTrainer)
            .Include(x => x.ScheduleHistory).FirstOrDefaultAsync(x => x.Id == model.Id);
        if (request is null) return null;
        model.Request = request;
        model.Trainers = await dbContext.Trainers.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.FirstName)
            .Select(x => new SelectListItem(x.FirstName + " " + x.LastName, x.Id.ToString())).ToListAsync();
        return model;
    }
}
