using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Services;
using NO23.Web.ViewModels.Member;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Web.Areas.Trainer.Controllers;

[Area("Trainer")]
[Authorize(Roles = ApplicationRoles.Trainer)]
public class MeasurementsController(
    ApplicationDbContext db,
    MemberProgressTrackingService progress) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? memberId, DateOnly? date)
    {
        var trainerId = await GetTrainerIdAsync();
        if (trainerId is null) return Forbid();

        var page = await LoadAsync(trainerId.Value, memberId, date);
        if (memberId.HasValue && page.MemberId is null) return NotFound();
        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        int memberId,
        [Bind(Prefix = "Input")] MemberProgressEntryInputViewModel input)
    {
        var trainerId = await GetTrainerIdAsync();
        if (trainerId is null) return Forbid();

        var member = await db.MemberProfiles
            .Where(item => item.Id == memberId && item.AssignedTrainerId == trainerId.Value)
            .Select(item => new { item.ApplicationUserId })
            .FirstOrDefaultAsync();

        if (member is null) return NotFound();

        if (input.EntryDate == default || !HasMeasurement(input))
        {
            ModelState.AddModelError(
                string.Empty,
                "Tarih ve en az bir ölçüm değeri girmelisiniz.");
        }

        if (ModelState.IsValid)
        {
            var result = await progress.UpsertAsync(
                member.ApplicationUserId,
                input,
                preserveCaloriesWhenMissing: true);
            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "Üye ölçümü kaydedildi.";
                return RedirectToAction(nameof(Index), new { memberId });
            }

            ModelState.AddModelError(string.Empty, result.Message);
        }

        var page = await LoadAsync(trainerId.Value, memberId, input.EntryDate);
        page.Input = input;
        return View("Index", page);
    }

    private async Task<int?> GetTrainerIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return null;

        return await db.Trainers
            .Where(item => item.ApplicationUserId == userId && item.IsActive)
            .Select(item => (int?)item.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<TrainerMeasurementsViewModel> LoadAsync(
        int trainerId,
        int? memberId,
        DateOnly? date)
    {
        var page = new TrainerMeasurementsViewModel
        {
            Members = await db.MemberProfiles
                .AsNoTracking()
                .Where(item => item.AssignedTrainerId == trainerId)
                .OrderBy(item => item.ApplicationUser.FirstName)
                .ThenBy(item => item.ApplicationUser.LastName)
                .Select(item => new SelectListItem(
                    item.ApplicationUser.FirstName + " " + item.ApplicationUser.LastName,
                    item.Id.ToString()))
                .ToListAsync()
        };

        if (!memberId.HasValue) return page;

        var member = await db.MemberProfiles
            .AsNoTracking()
            .Where(item => item.Id == memberId && item.AssignedTrainerId == trainerId)
            .Select(item => new
            {
                item.Id,
                Name = item.ApplicationUser.FirstName + " " + item.ApplicationUser.LastName
            })
            .FirstOrDefaultAsync();

        if (member is null) return page;

        var entries = await db.MemberProgressEntries
            .AsNoTracking()
            .Where(item => item.MemberProfileId == member.Id &&
                (item.BodyWeightKg.HasValue || item.HeightCm.HasValue ||
                 item.ShoulderCm.HasValue || item.ChestCm.HasValue ||
                 item.RightArmCm.HasValue || item.LeftArmCm.HasValue ||
                 item.WaistCm.HasValue || item.AbdomenCm.HasValue ||
                 item.HipCm.HasValue || item.RightUpperLegCm.HasValue ||
                 item.LeftUpperLegCm.HasValue || item.BodyFatPercent.HasValue ||
                 item.MuscleMassPercent.HasValue || item.DailyWaterIntakeLiters.HasValue))
            .OrderByDescending(item => item.EntryDate)
            .ToListAsync();

        var selectedDate = date ?? DateOnly.FromDateTime(ClubTime.Now);
        var entry = entries.FirstOrDefault(item => item.EntryDate == selectedDate);

        page = new TrainerMeasurementsViewModel
        {
            Members = page.Members,
            MemberId = member.Id,
            MemberName = member.Name.Trim(),
            Entries = entries,
            Input = new MemberProgressEntryInputViewModel
            {
                EntryDate = selectedDate,
                CaloriesConsumed = entry?.CaloriesConsumed,
                BodyWeightKg = entry?.BodyWeightKg,
                HeightCm = entry?.HeightCm,
                ShoulderCm = entry?.ShoulderCm,
                ChestCm = entry?.ChestCm,
                RightArmCm = entry?.RightArmCm,
                LeftArmCm = entry?.LeftArmCm,
                WaistCm = entry?.WaistCm,
                AbdomenCm = entry?.AbdomenCm,
                HipCm = entry?.HipCm,
                RightUpperLegCm = entry?.RightUpperLegCm,
                LeftUpperLegCm = entry?.LeftUpperLegCm,
                BodyFatPercent = entry?.BodyFatPercent,
                MuscleMassPercent = entry?.MuscleMassPercent,
                DailyWaterIntakeLiters = entry?.DailyWaterIntakeLiters
            }
        };

        return page;
    }

    private static bool HasMeasurement(MemberProgressEntryInputViewModel input)
    {
        return input.CaloriesConsumed.HasValue || input.BodyWeightKg.HasValue || input.HeightCm.HasValue ||
               input.ShoulderCm.HasValue || input.ChestCm.HasValue ||
               input.RightArmCm.HasValue || input.LeftArmCm.HasValue ||
               input.WaistCm.HasValue || input.AbdomenCm.HasValue ||
               input.HipCm.HasValue || input.RightUpperLegCm.HasValue ||
               input.LeftUpperLegCm.HasValue || input.BodyFatPercent.HasValue ||
               input.MuscleMassPercent.HasValue || input.DailyWaterIntakeLiters.HasValue;
    }

}
