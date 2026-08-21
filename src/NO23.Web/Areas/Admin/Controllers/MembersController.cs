using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class MembersController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var members = await dbContext.MemberProfiles
            .AsNoTracking()
            .Include(profile => profile.ApplicationUser)
            .Include(profile => profile.MembershipPackage)
            .OrderByDescending(profile => profile.CreatedAtUtc)
            .Select(profile => new MemberListItemViewModel
            {
                Id = profile.ApplicationUserId,
                MemberProfileId = profile.Id,
                FullName = ((profile.ApplicationUser.FirstName ?? "") + " " + (profile.ApplicationUser.LastName ?? "")).Trim(),
                Email = profile.ApplicationUser.Email ?? "",
                PhoneNumber = profile.ApplicationUser.PhoneNumber,
                PackageName = profile.MembershipPackage.Name,
                FitnessGoal = profile.FitnessGoal,
                RemainingClassCredits = profile.RemainingClassCredits,
                IsUnlimitedPackage = profile.MembershipPackage.WeeklyClassLimit == null,
                AssignedTrainerId = profile.AssignedTrainerId,
                AssignedTrainerName = profile.AssignedTrainer == null
                    ? null
                    : profile.AssignedTrainer.FirstName + " " + profile.AssignedTrainer.LastName,
                CreatedAtUtc = profile.CreatedAtUtc
            })
            .ToListAsync();

        ViewBag.Trainers = new SelectList(
            await dbContext.Trainers.AsNoTracking().Where(item => item.IsActive)
                .OrderBy(item => item.FirstName).ThenBy(item => item.LastName)
                .Select(item => new { item.Id, Name = item.FirstName + " " + item.LastName })
                .ToListAsync(), "Id", "Name");
        return View(members);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await dbContext.MemberProfiles.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new MemberEditViewModel
            {
                Id = item.Id,
                FirstName = item.ApplicationUser.FirstName ?? string.Empty,
                LastName = item.ApplicationUser.LastName ?? string.Empty,
                Email = item.ApplicationUser.Email ?? string.Empty,
                PhoneNumber = item.ApplicationUser.PhoneNumber,
                MembershipPackageId = item.MembershipPackageId,
                FitnessGoal = item.FitnessGoal,
                RemainingClassCredits = item.RemainingClassCredits,
                AssignedTrainerId = item.AssignedTrainerId
            }).FirstOrDefaultAsync();
        if (model is null) return NotFound();

        await LoadEditOptionsAsync(model.MembershipPackageId, model.AssignedTrainerId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MemberEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var packageExists = await dbContext.MembershipPackages
            .AnyAsync(item => item.Id == model.MembershipPackageId &&
                (item.IsActive || item.MemberProfiles.Any(profile => profile.Id == id)));
        var trainerExists = model.AssignedTrainerId is null || await dbContext.Trainers
            .AnyAsync(item => item.Id == model.AssignedTrainerId &&
                (item.IsActive || item.AssignedMembers.Any(profile => profile.Id == id)));
        if (!packageExists) ModelState.AddModelError(nameof(model.MembershipPackageId), "Aktif bir paket seçmelisiniz.");
        if (!trainerExists) ModelState.AddModelError(nameof(model.AssignedTrainerId), "Aktif bir trainer seçmelisiniz.");

        var member = await dbContext.MemberProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (member is null) return NotFound();

        var email = model.Email.Trim();
        var emailOwner = await userManager.FindByEmailAsync(email);
        if (emailOwner is not null && emailOwner.Id != member.ApplicationUserId)
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi başka bir hesap tarafından kullanılıyor.");

        if (!ModelState.IsValid)
        {
            await LoadEditOptionsAsync(model.MembershipPackageId, model.AssignedTrainerId);
            return View(model);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var user = member.ApplicationUser;
        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.Email = email;
        user.UserName = email;
        user.NormalizedEmail = userManager.NormalizeEmail(email);
        user.NormalizedUserName = userManager.NormalizeName(email);
        user.EmailConfirmed = true;
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

        member.MembershipPackageId = model.MembershipPackageId;
        member.FitnessGoal = model.FitnessGoal?.Trim();
        member.RemainingClassCredits = model.RemainingClassCredits;
        member.AssignedTrainerId = model.AssignedTrainerId;
        member.UpdatedAtUtc = DateTime.UtcNow;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            await LoadEditOptionsAsync(model.MembershipPackageId, model.AssignedTrainerId);
            return View(model);
        }
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["StatusMessage"] = "Üye bilgileri güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await BuildDeleteModelAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var member = await dbContext.MemberProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (member is null) return NotFound();

        var protectedHistory = await HasProtectedHistoryAsync(id);
        if (protectedHistory)
        {
            TempData["ErrorMessage"] =
                "Bu üyenin sipariş, mutfak aboneliği veya birebir ders geçmişi bulunduğu için kalıcı olarak silinemez.";
            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var result = await userManager.DeleteAsync(member.ApplicationUser);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(item => item.Description));
            return RedirectToAction(nameof(Index));
        }
        await transaction.CommitAsync();
        TempData["StatusMessage"] = "Üye ve giriş hesabı kalıcı olarak silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTrainer(int memberProfileId, int? trainerId)
    {
        var member = await dbContext.MemberProfiles.FindAsync(memberProfileId);
        if (member is null) return NotFound();
        if (trainerId is not null && !await dbContext.Trainers.AnyAsync(item => item.Id == trainerId && item.IsActive))
            return BadRequest();

        member.AssignedTrainerId = trainerId;
        member.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = trainerId is null ? "Antrenör ataması kaldırıldı." : "Antrenör üyeye atandı.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadEditOptionsAsync(int packageId, int? trainerId)
    {
        ViewBag.Packages = new SelectList(
            await dbContext.MembershipPackages.AsNoTracking().Where(item => item.IsActive || item.Id == packageId)
                .OrderBy(item => item.DisplayOrder).Select(item => new { item.Id, item.Name }).ToListAsync(),
            "Id", "Name", packageId);
        ViewBag.Trainers = new SelectList(
            await dbContext.Trainers.AsNoTracking().Where(item => item.IsActive || item.Id == trainerId)
                .OrderBy(item => item.FirstName).ThenBy(item => item.LastName)
                .Select(item => new { item.Id, Name = item.FirstName + " " + item.LastName }).ToListAsync(),
            "Id", "Name", trainerId);
    }

    private async Task<MemberDeleteViewModel?> BuildDeleteModelAsync(int id)
    {
        return await dbContext.MemberProfiles.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new MemberDeleteViewModel
            {
                Id = item.Id,
                FullName = ((item.ApplicationUser.FirstName ?? "") + " " + (item.ApplicationUser.LastName ?? "")).Trim(),
                Email = item.ApplicationUser.Email ?? string.Empty,
                PackageName = item.MembershipPackage.Name,
                OrderCount = item.Orders.Count,
                PersonalTrainingSessionCount = item.PersonalTrainingSessions.Count,
                KitchenSubscriptionCount = item.KitchenSubscriptions.Count,
                HasProtectedHistory = item.Orders.Any() || item.PersonalTrainingSessions.Any() || item.KitchenSubscriptions.Any()
            }).FirstOrDefaultAsync();
    }

    private async Task<bool> HasProtectedHistoryAsync(int id) =>
        await dbContext.MemberProfiles.AnyAsync(item => item.Id == id &&
            (item.Orders.Any() || item.PersonalTrainingSessions.Any() || item.KitchenSubscriptions.Any()));
}
