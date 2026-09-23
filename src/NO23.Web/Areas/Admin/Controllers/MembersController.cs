using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using NO23.Web.Domain.Entities;
using NO23.Web.Domain.Enums;
using NO23.Web.Services;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class MembersController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new MemberCreateViewModel();
        await LoadCreateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MemberCreateViewModel model)
    {
        var variant = await FindSelectableVariantAsync(model.ServicePackageVariantId);
        if (variant is null)
            ModelState.AddModelError(nameof(model.ServicePackageVariantId), "Aktif bir üyelik, grup dersi veya Kids paketi seçmelisiniz.");
        var family = await ResolveKidsFamilyAsync(variant, model.FamilyCode);
        if (family.Error is not null)
            ModelState.AddModelError(nameof(model.FamilyCode), family.Error);
        if (await userManager.FindByEmailAsync(model.Email.Trim()) is not null)
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi zaten kullanılıyor.");
        if (!ModelState.IsValid)
        {
            await LoadCreateOptionsAsync(model);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(), Email = model.Email.Trim(), EmailConfirmed = true,
            FirstName = model.FirstName.Trim(), LastName = model.LastName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim()
        };
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            await LoadCreateOptionsAsync(model);
            return View(model);
        }
        await userManager.AddToRoleAsync(user, ApplicationRoles.Member);
        var membershipPackageId = await ResolveLegacyMembershipPackageIdAsync(variant!);
        var membershipStartsAtUtc = variant!.ServicePackage.Category == ServicePackageCategory.Membership
            ? DateTime.UtcNow : (DateTime?)null;
        dbContext.MemberProfiles.Add(new MemberProfile
        {
            ApplicationUserId = user.Id,
            MembershipPackageId = membershipPackageId,
            ServicePackageVariantId = variant!.Id,
            FitnessGoal = model.FitnessGoal?.Trim(),
            RemainingClassCredits = MemberPackageEntitlement.CalculateInitialCredits(variant),
            MembershipStartsAtUtc = membershipStartsAtUtc,
            MembershipEndsAtUtc = membershipStartsAtUtc.HasValue
                ? MemberPackageEntitlement.CalculateEndDate(variant, membershipStartsAtUtc.Value)
                : null,
            AssignedTrainerId = model.AssignedTrainerId,
            FamilyCode = family.Code,
            SiblingDiscountPercent = family.DiscountPercent,
            ReferralCode = $"NO23-{Guid.NewGuid():N}"[..13].ToUpperInvariant()
        });
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = family.Code is null
            ? "Üye hesabı ve paket hakları oluşturuldu. Geçici parolayı üyeyle paylaşabilirsiniz."
            : $"Üye hesabı ve Kids paketi oluşturuldu. Aile kodu: {family.Code}";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Index()
    {
        var members = await dbContext.MemberProfiles
            .AsNoTracking()
            .Include(profile => profile.ApplicationUser)
            .Include(profile => profile.MembershipPackage)
            .Include(profile => profile.ServicePackageVariant)
                .ThenInclude(variant => variant!.ServicePackage)
            .OrderByDescending(profile => profile.CreatedAtUtc)
            .Select(profile => new MemberListItemViewModel
            {
                Id = profile.ApplicationUserId,
                MemberProfileId = profile.Id,
                FullName = ((profile.ApplicationUser.FirstName ?? "") + " " + (profile.ApplicationUser.LastName ?? "")).Trim(),
                Email = profile.ApplicationUser.Email ?? "",
                PhoneNumber = profile.ApplicationUser.PhoneNumber,
                PackageName = profile.ServicePackageVariant == null
                    ? dbContext.ServicePackages
                    .Where(package => package.Category == ServicePackageCategory.Membership && package.IsActive &&
                        package.MembershipPackageId == profile.MembershipPackageId)
                    .OrderBy(package => package.DisplayOrder)
                    .Select(package => package.Name)
                    .FirstOrDefault() ?? profile.MembershipPackage.Name
                    : profile.ServicePackageVariant.ServicePackage.Name + " — " + profile.ServicePackageVariant.Name,
                FamilyCode = profile.FamilyCode,
                SiblingDiscountPercent = profile.SiblingDiscountPercent,
                FitnessGoal = profile.FitnessGoal,
                RemainingClassCredits = profile.RemainingClassCredits,
                MembershipEndsAtUtc = profile.MembershipEndsAtUtc,
                IsUnlimitedPackage = profile.ServicePackageVariantId == null && profile.MembershipPackage.WeeklyClassLimit == null,
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
                ServicePackageVariantId = item.ServicePackageVariantId,
                MembershipEndsOn = item.MembershipEndsAtUtc.HasValue
                    ? DateOnly.FromDateTime(ClubTime.ToLocal(item.MembershipEndsAtUtc.Value))
                    : null,
                FamilyCode = item.FamilyCode,
                FitnessGoal = item.FitnessGoal,
                RemainingClassCredits = item.RemainingClassCredits,
                AssignedTrainerId = item.AssignedTrainerId
            }).FirstOrDefaultAsync();
        if (model is null) return NotFound();

        await LoadEditOptionsAsync(model.ServicePackageVariantId, model.AssignedTrainerId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MemberEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var member = await dbContext.MemberProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (member is null) return NotFound();

        var variant = model.ServicePackageVariantId is null && member.ServicePackageVariantId is null
            ? null
            : await FindSelectableVariantAsync(model.ServicePackageVariantId);
        var packageExists = model.ServicePackageVariantId is null
            ? member.ServicePackageVariantId is null
            : variant is not null;
        var trainerExists = model.AssignedTrainerId is null || await dbContext.Trainers
            .AnyAsync(item => item.Id == model.AssignedTrainerId &&
                (item.IsActive || item.AssignedMembers.Any(profile => profile.Id == id)));
        if (!packageExists)
            ModelState.AddModelError(nameof(model.ServicePackageVariantId), "Aktif bir üyelik, grup dersi veya Kids paketi seçmelisiniz.");
        if (!trainerExists) ModelState.AddModelError(nameof(model.AssignedTrainerId), "Aktif bir trainer seçmelisiniz.");

        var email = model.Email.Trim();
        var emailOwner = await userManager.FindByEmailAsync(email);
        if (emailOwner is not null && emailOwner.Id != member.ApplicationUserId)
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi başka bir hesap tarafından kullanılıyor.");

        if (!ModelState.IsValid)
        {
            await LoadEditOptionsAsync(model.ServicePackageVariantId, model.AssignedTrainerId);
            return View(model);
        }

        var family = await ResolveKidsFamilyAsync(
            variant, model.FamilyCode, member.Id, member.FamilyCode, member.SiblingDiscountPercent);
        if (family.Error is not null)
        {
            ModelState.AddModelError(nameof(model.FamilyCode), family.Error);
            await LoadEditOptionsAsync(model.ServicePackageVariantId, model.AssignedTrainerId);
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

        var packageChanged = member.ServicePackageVariantId != model.ServicePackageVariantId;
        if (variant is not null)
        {
            member.MembershipPackageId = await ResolveLegacyMembershipPackageIdAsync(variant);
            if (packageChanged)
            {
                member.RemainingClassCredits = MemberPackageEntitlement.CalculateInitialCredits(variant);
                member.LastMembershipOrderId = null;
                member.MembershipStartsAtUtc = variant.ServicePackage.Category == ServicePackageCategory.Membership
                    ? DateTime.UtcNow : null;
                member.MembershipEndsAtUtc = member.MembershipStartsAtUtc.HasValue
                    ? MemberPackageEntitlement.CalculateEndDate(variant, member.MembershipStartsAtUtc.Value)
                    : null;
            }
        }
        member.ServicePackageVariantId = model.ServicePackageVariantId;
        if (!packageChanged)
        {
            var existingEndDay = member.MembershipEndsAtUtc.HasValue
                ? DateOnly.FromDateTime(ClubTime.ToLocal(member.MembershipEndsAtUtc.Value))
                : (DateOnly?)null;
            if (model.MembershipEndsOn != existingEndDay)
                member.MembershipEndsAtUtc = model.MembershipEndsOn.HasValue
                    ? ClubTime.ToUtc(model.MembershipEndsOn.Value.ToDateTime(TimeOnly.MaxValue))
                    : null;
        }
        member.FamilyCode = family.Code;
        member.SiblingDiscountPercent = family.DiscountPercent;
        member.FitnessGoal = model.FitnessGoal?.Trim();
        if (!packageChanged)
            member.RemainingClassCredits = model.RemainingClassCredits;
        member.AssignedTrainerId = model.AssignedTrainerId;
        member.UpdatedAtUtc = DateTime.UtcNow;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            await LoadEditOptionsAsync(model.ServicePackageVariantId, model.AssignedTrainerId);
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

    private async Task LoadEditOptionsAsync(int? variantId, int? trainerId)
    {
        ViewBag.PackageOptions = await ManualPackageOptionsAsync(variantId);
        ViewBag.Trainers = new SelectList(
            await dbContext.Trainers.AsNoTracking().Where(item => item.IsActive || item.Id == trainerId)
                .OrderBy(item => item.FirstName).ThenBy(item => item.LastName)
                .Select(item => new { item.Id, Name = item.FirstName + " " + item.LastName }).ToListAsync(),
            "Id", "Name", trainerId);
    }

    private async Task LoadCreateOptionsAsync(MemberCreateViewModel model)
    {
        ViewBag.PackageOptions = await ManualPackageOptionsAsync(model.ServicePackageVariantId);
        ViewBag.Trainers = new SelectList(await dbContext.Trainers.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.FirstName).ThenBy(x => x.LastName).Select(x => new { x.Id, Name = x.FirstName + " " + x.LastName }).ToListAsync(), "Id", "Name", model.AssignedTrainerId);
    }

    private async Task<MemberDeleteViewModel?> BuildDeleteModelAsync(int id)
    {
        return await dbContext.MemberProfiles.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new MemberDeleteViewModel
            {
                Id = item.Id,
                FullName = ((item.ApplicationUser.FirstName ?? "") + " " + (item.ApplicationUser.LastName ?? "")).Trim(),
                Email = item.ApplicationUser.Email ?? string.Empty,
                PackageName = item.ServicePackageVariant == null
                    ? dbContext.ServicePackages
                    .Where(package => package.Category == ServicePackageCategory.Membership && package.IsActive &&
                        package.MembershipPackageId == item.MembershipPackageId)
                    .OrderBy(package => package.DisplayOrder)
                    .Select(package => package.Name)
                    .FirstOrDefault() ?? item.MembershipPackage.Name
                    : item.ServicePackageVariant.ServicePackage.Name + " — " + item.ServicePackageVariant.Name,
                OrderCount = item.Orders.Count,
                PersonalTrainingSessionCount = item.PersonalTrainingSessions.Count,
                KitchenSubscriptionCount = item.KitchenSubscriptions.Count,
                HasProtectedHistory = item.Orders.Any() || item.PersonalTrainingSessions.Any() || item.KitchenSubscriptions.Any()
            }).FirstOrDefaultAsync();
    }

    private async Task<bool> HasProtectedHistoryAsync(int id) =>
        await dbContext.MemberProfiles.AnyAsync(item => item.Id == id &&
            (item.Orders.Any() || item.PersonalTrainingSessions.Any() || item.KitchenSubscriptions.Any()));

    private async Task<List<ManualMemberPackageOptionViewModel>> ManualPackageOptionsAsync(int? selectedId)
    {
        var variants = await dbContext.ServicePackageVariants.AsNoTracking()
            .Where(item => (item.IsActive || item.Id == selectedId) && item.ServicePackage.IsActive &&
                (item.ServicePackage.Category == ServicePackageCategory.Membership ||
                 item.ServicePackage.Category == ServicePackageCategory.GroupClasses ||
                 item.ServicePackage.Category == ServicePackageCategory.KidsClub))
            .OrderBy(item => item.ServicePackage.Category)
            .ThenBy(item => item.ServicePackage.DisplayOrder)
            .ThenBy(item => item.DisplayOrder)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.TotalPrice,
                item.ServicePackage.Category,
                PackageName = item.ServicePackage.Name
            }).ToListAsync();

        return variants.Select(item => new ManualMemberPackageOptionViewModel
        {
            Id = item.Id,
            Category = item.Category.ToString(),
            GroupName = item.Category switch
            {
                ServicePackageCategory.Membership => "Üyelik paketleri",
                ServicePackageCategory.GroupClasses => "Grup dersi paketleri",
                _ => "Kids paketleri"
            },
            Label = $"{item.PackageName} — {item.Name} — {item.TotalPrice:N0} ₺"
        }).ToList();
    }

    private async Task<ServicePackageVariant?> FindSelectableVariantAsync(int? variantId)
    {
        if (variantId is null) return null;
        return await dbContext.ServicePackageVariants
            .Include(item => item.ServicePackage)
            .FirstOrDefaultAsync(item => item.Id == variantId && item.IsActive && item.ServicePackage.IsActive &&
                (item.ServicePackage.Category == ServicePackageCategory.Membership ||
                 item.ServicePackage.Category == ServicePackageCategory.GroupClasses ||
                 item.ServicePackage.Category == ServicePackageCategory.KidsClub));
    }

    private async Task<int> ResolveLegacyMembershipPackageIdAsync(ServicePackageVariant variant)
    {
        if (variant.ServicePackage.MembershipPackageId is int packageId) return packageId;
        return await dbContext.MembershipPackages
            .Where(item => item.Code == MembershipPackageCode.Start)
            .Select(item => item.Id)
            .SingleAsync();
    }

    private async Task<(string? Code, int DiscountPercent, string? Error)> ResolveKidsFamilyAsync(
        ServicePackageVariant? variant, string? submittedCode, int? currentMemberId = null,
        string? currentCode = null, int currentDiscountPercent = 0)
    {
        if (variant?.ServicePackage.Category != ServicePackageCategory.KidsClub)
            return (null, 0, null);

        var code = submittedCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            return ($"KIDS-{Guid.NewGuid():N}"[..13].ToUpperInvariant(), 0, null);

        if (string.Equals(code, currentCode, StringComparison.OrdinalIgnoreCase))
            return (code, currentDiscountPercent, null);

        var familyExists = await dbContext.MemberProfiles.AnyAsync(item =>
            item.Id != currentMemberId && item.FamilyCode == code &&
            item.ServicePackageVariant != null &&
            item.ServicePackageVariant.ServicePackage.Category == ServicePackageCategory.KidsClub);
        return familyExists
            ? (code, 25, null)
            : (null, 0, "Girilen aile koduna bağlı aktif bir Kids üyesi bulunamadı.");
    }
}
