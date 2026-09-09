using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.Services;
using NO23.Web.ViewModels.Admin;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = ApplicationRoles.Admin)]
public class MemberProgressController(ApplicationDbContext db, MemberProgressTrackingService progress) : Controller
{
    public async Task<IActionResult> Index(int? memberId, DateOnly? date, int? before, int? after)
    {
        var page = await LoadAsync(memberId, date);
        if (memberId.HasValue && page.MemberId is null) return NotFound();
        page.Before = page.Photos.Any(x => x.Id == before) ? before : page.Photos.FirstOrDefault()?.Id;
        page.After = page.Photos.Any(x => x.Id == after) ? after : page.Photos.LastOrDefault()?.Id;
        return View(page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int memberId, [Bind(Prefix = "Input")] MemberProgressEntryInputViewModel input)
    {
        var userId = await db.MemberProfiles.Where(x => x.Id == memberId).Select(x => x.ApplicationUserId).FirstOrDefaultAsync();
        if (userId is null) return NotFound();
        if (input.EntryDate == default || new decimal?[] { input.BodyWeightKg, input.BodyFatKg, input.BodyFatPercent,
                input.MuscleMassKg, input.MuscleMassPercent, input.BodyWaterAmount, input.BodyWaterPercent }.All(x => x is null))
            ModelState.AddModelError(string.Empty, "Tarih ve en az bir ölçüm değeri girmelisin.");
        // Staff measurements must not erase the member's separately logged calories.
        input.CaloriesConsumed = await db.MemberProgressEntries.Where(x => x.MemberProfileId == memberId && x.EntryDate == input.EntryDate)
            .Select(x => x.CaloriesConsumed).FirstOrDefaultAsync();
        if (ModelState.IsValid)
        {
            var result = await progress.UpsertAsync(userId, input);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Ölçüm kaydedildi. Önceki kayıtlarla karşılaştırabilirsin.";
                return RedirectToAction(nameof(Index), new { memberId });
            }
            ModelState.AddModelError(string.Empty, result.Message);
        }
        var page = await LoadAsync(memberId, input.EntryDate);
        page.Input = input;
        return View("Index", page);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(ProgressPhotoInput input)
    {
        if (!await db.MemberProfiles.AnyAsync(x => x.Id == input.MemberId)) return NotFound();
        if (!ModelState.IsValid || input.Photo is null || input.Photo.Length is <= 0 or > ProgressPhotoValidation.MaxBytes ||
            input.TakenOn == default || input.TakenOn > DateOnly.FromDateTime(ClubTime.Now))
            return UploadError(input.MemberId, "Geçerli bir tarih ve en fazla 5 MB boyutunda JPEG / PNG fotoğraf seç.");
        using var buffer = new MemoryStream();
        await input.Photo.CopyToAsync(buffer);
        var data = buffer.ToArray();
        var contentType = ProgressPhotoValidation.ContentType(data);
        if (contentType is null) return UploadError(input.MemberId, "Dosya JPEG veya PNG formatında olmalı.");
        db.MemberProgressPhotos.Add(new MemberProgressPhoto
            { MemberProfileId = input.MemberId, TakenOn = input.TakenOn, Caption = input.Caption?.Trim(), ContentType = contentType, Data = data });
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Gelişim fotoğrafı eklendi.";
        return RedirectToAction(nameof(Index), new { memberId = input.MemberId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int id, int memberId)
    {
        var photo = await db.MemberProgressPhotos.FirstOrDefaultAsync(x => x.Id == id && x.MemberProfileId == memberId);
        if (photo is null) return NotFound();
        db.MemberProgressPhotos.Remove(photo);
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Fotoğraf silindi.";
        return RedirectToAction(nameof(Index), new { memberId });
    }

    private IActionResult UploadError(int memberId, string message)
    {
        TempData["ErrorMessage"] = message;
        return RedirectToAction(nameof(Index), new { memberId });
    }

    private async Task<MemberProgressPage> LoadAsync(int? memberId, DateOnly? date)
    {
        var page = new MemberProgressPage
        {
            Members = await db.MemberProfiles.AsNoTracking().OrderBy(x => x.ApplicationUser.FirstName)
                .Select(x => new SelectListItem(x.ApplicationUser.FirstName + " " + x.ApplicationUser.LastName + " — " + x.ApplicationUser.Email, x.Id.ToString())).ToListAsync()
        };
        if (!memberId.HasValue) return page;
        var member = await db.MemberProfiles.Include(x => x.ApplicationUser).AsNoTracking().FirstOrDefaultAsync(x => x.Id == memberId);
        if (member is null) return page;
        page.MemberId = member.Id;
        page.MemberName = member.ApplicationUser.FirstName + " " + member.ApplicationUser.LastName;
        page.Entries = await db.MemberProgressEntries.AsNoTracking().Where(x => x.MemberProfileId == member.Id &&
            (x.BodyWeightKg != null || x.BodyFatKg != null || x.BodyFatPercent != null || x.MuscleMassKg != null ||
             x.MuscleMassPercent != null || x.BodyWaterAmount != null || x.BodyWaterPercent != null))
            .OrderByDescending(x => x.EntryDate).ToListAsync();
        page.Photos = await db.MemberProgressPhotos.AsNoTracking().Where(x => x.MemberProfileId == member.Id)
            .OrderBy(x => x.TakenOn).ThenBy(x => x.Id).Select(x => new ProgressPhotoInfo(x.Id, x.TakenOn, x.Caption)).ToListAsync();
        var selectedDate = date ?? DateOnly.FromDateTime(ClubTime.Now);
        var entry = page.Entries.FirstOrDefault(x => x.EntryDate == selectedDate);
        page.Input = new MemberProgressEntryInputViewModel
        {
            EntryDate = selectedDate, BodyWeightKg = entry?.BodyWeightKg, BodyFatKg = entry?.BodyFatKg,
            BodyFatPercent = entry?.BodyFatPercent, MuscleMassKg = entry?.MuscleMassKg, MuscleMassPercent = entry?.MuscleMassPercent,
            BodyWaterAmount = entry?.BodyWaterAmount, BodyWaterPercent = entry?.BodyWaterPercent
        };
        return page;
    }
}
