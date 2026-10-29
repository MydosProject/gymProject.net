using System.Globalization;
using System.Security.Claims;
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
using NO23.Web.Infrastructure.Identity;
using NO23.Web.Services;
using NO23.Web.Infrastructure.Validation;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class MembersController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var member = await dbContext.MemberProfiles.AsNoTracking().Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);
        return member is null ? NotFound() : View(new MemberPasswordResetViewModel
        {
            Id = id, MemberName = $"{member.ApplicationUser.FirstName} {member.ApplicationUser.LastName}".Trim()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, MemberPasswordResetViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var member = await dbContext.MemberProfiles.Include(item => item.ApplicationUser).FirstOrDefaultAsync(item => item.Id == id);
        if (member is null) return NotFound();
        model.MemberName = $"{member.ApplicationUser.FirstName} {member.ApplicationUser.LastName}".Trim();
        if (model.Password != model.ConfirmPassword) ModelState.AddModelError(nameof(model.ConfirmPassword), "Parolalar aynı olmalıdır.");
        if (!ModelState.IsValid) return View(model);
        var token = await userManager.GeneratePasswordResetTokenAsync(member.ApplicationUser);
        var result = await userManager.ResetPasswordAsync(member.ApplicationUser, token, model.Password);
        if (!result.Succeeded)
        {
            model.Password = model.ConfirmPassword = string.Empty;
            ModelState.Remove(nameof(model.Password));
            foreach (var error in result.Errors) ModelState.AddModelError(nameof(model.Password), error.Description);
            ModelState.Remove(nameof(model.ConfirmPassword));
            return View(model);
        }
        await userManager.ResetAccessFailedCountAsync(member.ApplicationUser);
        await userManager.SetLockoutEndDateAsync(member.ApplicationUser, null);
        TempData["SuccessMessage"] = "Üyenin geçici parolası yenilendi. Gerektiğinde yeniden yenileyebilirsiniz.";
        return RedirectToAction(nameof(Index));
    }

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
        model.FirstName = MemberLoginName.CleanPart(model.FirstName ?? string.Empty);
        model.LastName = MemberLoginName.CleanPart(model.LastName ?? string.Empty);
        model.NationalIdentityNumber = model.NationalIdentityNumber?.Trim() ?? string.Empty;
        model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();

        var birthDateError = BirthDateValidation.GetError(model.BirthDate, required: true);
        if (birthDateError is not null)
            ModelState.AddModelError(nameof(model.BirthDate), birthDateError);

        var variant = await FindSelectableVariantAsync(model.ServicePackageVariantId);
        if (variant is null)
            ModelState.AddModelError(nameof(model.ServicePackageVariantId), "Aktif bir üyelik, grup dersi veya Kids paketi seçmelisiniz.");
        var family = await ResolveKidsFamilyAsync(variant, model.FamilyCode);
        if (family.Error is not null)
            ModelState.AddModelError(nameof(model.FamilyCode), family.Error);
        var payment = variant is null ? (Order: (Order?)null, Errors: (IReadOnlyList<System.ComponentModel.DataAnnotations.ValidationResult>)[])
            : ManualOrderPaymentService.PreparePackageOrder(variant, model.Payment ?? new(), family.DiscountPercent,
                User.FindFirstValue(ClaimTypes.NameIdentifier));
        foreach (var error in payment.Errors)
            foreach (var field in error.MemberNames)
                ModelState.AddModelError(field, error.ErrorMessage!);
        if (model.Email is not null && await userManager.FindByEmailAsync(model.Email) is not null)
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi zaten kullanılıyor.");
        if (await dbContext.MemberProfiles.AnyAsync(profile =>
                profile.NationalIdentityNumber == model.NationalIdentityNumber))
            ModelState.AddModelError(nameof(model.NationalIdentityNumber), "Bu TC Kimlik No ile daha önce bir üyelik oluşturulmuş.");

        var memberUserName = MemberLoginName.BuildUserName(model.FirstName, model.LastName);
        if (await userManager.FindByNameAsync(memberUserName) is not null)
            ModelState.AddModelError(string.Empty, "Bu ad soyad ile daha önce bir üyelik oluşturulmuş.");

        if (!ModelState.IsValid)
        {
            await LoadCreateOptionsAsync(model);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = memberUserName,
            Email = model.Email,
            EmailConfirmed = model.Email is not null,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim()
        };

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync()
            : null;
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            await LoadCreateOptionsAsync(model);
            return View(model);
        }
        var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.Member);
        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
            await LoadCreateOptionsAsync(model);
            return View(model);
        }
        var membershipPackageId = await ResolveLegacyMembershipPackageIdAsync(variant!);
        var membershipStartsAtUtc = DateTime.UtcNow;
        var profile = new MemberProfile
        {
            ApplicationUserId = user.Id,
            NationalIdentityNumber = model.NationalIdentityNumber,
            BirthDate = model.BirthDate,
            MembershipPackageId = membershipPackageId,
            ServicePackageVariantId = variant!.Id,
            FitnessGoal = model.FitnessGoal?.Trim(),
            RemainingClassCredits = MemberPackageEntitlement.CalculateInitialCredits(variant),
            MembershipStartsAtUtc = membershipStartsAtUtc,
            MembershipEndsAtUtc = MemberPackageEntitlement.CalculateEndDate(
                variant,
                membershipStartsAtUtc),
            AssignedTrainerId = model.AssignedTrainerId,
            FamilyCode = family.Code,
            SiblingDiscountPercent = family.DiscountPercent,
            ReferralCode = $"NO23-{Guid.NewGuid():N}"[..13].ToUpperInvariant()
        };
        dbContext.MemberProfiles.Add(profile);
        var paymentOrder = payment.Order!;
        paymentOrder.MemberProfile = profile;
        paymentOrder.DeliveryFullName = $"{model.FirstName} {model.LastName}";
        paymentOrder.DeliveryPhoneNumber = user.PhoneNumber ?? "-";
        dbContext.Orders.Add(paymentOrder);
        await dbContext.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        TempData["StatusMessage"] = family.Code is null
            ? "Üye hesabı ve paket hakları oluşturuldu. Geçici parolayı üyeyle paylaşabilirsiniz."
            : $"Üye hesabı ve Kids paketi oluşturuldu. Aile kodu: {family.Code}";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Index(int page = 1, string? query = null, string? state = null)
    {
        query = query?.Trim();
        if (!ModelState.IsValid || query?.Length > 80 || state is not (null or "" or "active" or "expired" or "kitchen")) return BadRequest();
        var allMembers = dbContext.MemberProfiles
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
                IsKitchenOnly = profile.MembershipPackage.Code == MembershipPackageCode.KitchenOnly,
                IsUnlimitedPackage = profile.MembershipPackage.Code != MembershipPackageCode.KitchenOnly &&
                    profile.ServicePackageVariantId == null && profile.MembershipPackage.WeeklyClassLimit == null,
                AssignedTrainerId = profile.AssignedTrainerId,
                AssignedTrainerName = profile.AssignedTrainer == null
                    ? null
                    : profile.AssignedTrainer.FirstName + " " + profile.AssignedTrainer.LastName,
                CreatedAtUtc = profile.CreatedAtUtc
            });
        ViewBag.OverallCount = await allMembers.CountAsync();
        ViewBag.PackageCount = await allMembers.Select(x => x.PackageName).Distinct().CountAsync();
        var month = ClubTime.ToUtc(new DateTime(ClubTime.Today.Year, ClubTime.Today.Month, 1));
        ViewBag.NewThisMonthCount = await allMembers.CountAsync(x => x.CreatedAtUtc >= month && x.CreatedAtUtc < month.AddMonths(1));
        var filtered = allMembers;
        if (!string.IsNullOrEmpty(query))
        {
            var normalized = query.ToLower(CultureInfo.GetCultureInfo("tr-TR"));
            int.TryParse(query.TrimStart('#'), out var memberId);
            var phone = new string(query.Where(char.IsDigit).ToArray());
            var numericPhone = phone.Length >= 2 && query.All(c => char.IsDigit(c) || "+- ()".Contains(c));
            filtered = filtered.Where(x => x.MemberProfileId == memberId ||
                x.FullName.Replace("I", "ı").Replace("İ", "i").ToLower().Contains(normalized) ||
                x.Email.ToLower().Contains(normalized) ||
                (numericPhone && x.PhoneNumber != null && x.PhoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(phone)));
        }
        var now = DateTime.UtcNow;
        filtered = state switch
        {
            "kitchen" => filtered.Where(x => x.IsKitchenOnly),
            "expired" => filtered.Where(x => !x.IsKitchenOnly && x.MembershipEndsAtUtc <= now),
            "active" => filtered.Where(x => !x.IsKitchenOnly && (!x.MembershipEndsAtUtc.HasValue || x.MembershipEndsAtUtc > now)),
            _ => filtered
        };
        var navigation = new NO23.Web.ViewModels.ListNavigationViewModel(await filtered.CountAsync(), page, query, state,
            [new("Süresi devam eden spor", "active"), new("Süresi biten spor", "expired"), new("Kitchen hesapları", "kitchen")]);
        ViewData["ListNavigation"] = navigation;
        var members = await filtered.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.MemberProfileId)
            .Skip(navigation.Skip).Take(NO23.Web.ViewModels.ListNavigationViewModel.PageSize).ToListAsync();

        var creditSummaries = await new LessonCreditSummaryService(dbContext).LoadAsync(members.Select(m => m.MemberProfileId));
        foreach (var member in members) member.Credits = creditSummaries.GetValueOrDefault(member.MemberProfileId);
        var payments = (await new MemberPaymentHistoryService(dbContext).LoadAsync(members.Select(m => m.MemberProfileId).ToArray()))
            .ToLookup(o => o.MemberId);
        foreach (var member in members) member.Payments = MemberPaymentSummary.Calculate(payments[member.MemberProfileId]);
        ViewBag.Trainers = new SelectList(
            await dbContext.Trainers.AsNoTracking().Where(item => item.IsActive)
                .OrderBy(item => item.FirstName).ThenBy(item => item.LastName)
                .Select(item => new { item.Id, Name = item.FirstName + " " + item.LastName })
                .ToListAsync(), "Id", "Name");
        return View(members);
    }

    [HttpGet]
    public async Task<IActionResult> Payments(int id)
    {
        var member = await dbContext.MemberProfiles.AsNoTracking().Where(m => m.Id == id)
            .Select(m => new { Name = m.ApplicationUser.FirstName + " " + m.ApplicationUser.LastName }).FirstOrDefaultAsync();
        if (member is null) return NotFound();
        return View(new MemberPaymentHistoryViewModel { MemberId = id, MemberName = member.Name,
            Orders = await new MemberPaymentHistoryService(dbContext).LoadAsync([id]) });
    }

    [HttpGet]
    public async Task<IActionResult> Credits(int id)
    {
        var member = await dbContext.MemberProfiles.AsNoTracking().Where(x => x.Id == id)
            .Select(x => x.ApplicationUser.FirstName + " " + x.ApplicationUser.LastName).FirstOrDefaultAsync();
        if (member is null) return NotFound();
        var service = new LessonCreditSummaryService(dbContext);
        return View(new MemberCreditsViewModel { MemberName = member, Summary = (await service.GetAsync(id))!, History = await service.HistoryAsync(id) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewCredits(int id, int expectedCount, int expectedBalance, bool confirmed, bool? alreadyAdjusted, bool cancelledGroups = false)
    {
        if (!ModelState.IsValid || alreadyAdjusted is null) return BadRequest();
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var service = new PersonalTrainingCalendarService(dbContext);
        var result = cancelledGroups
            ? await service.ReviewCancelledGroupCreditsAsync(id, expectedCount, expectedBalance, confirmed, actor, alreadyAdjusted.Value)
            : await service.ApplyReviewedCreditsAsync(id, expectedCount, expectedBalance, confirmed, actor, alreadyAdjusted.Value);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Credits), new { id });
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
                NationalIdentityNumber = item.NationalIdentityNumber ?? string.Empty,
                BirthDate = item.BirthDate,
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

        var birthDateError = BirthDateValidation.GetError(model.BirthDate);
        if (birthDateError is not null)
            ModelState.AddModelError(nameof(model.BirthDate), birthDateError);

        var variant = model.ServicePackageVariantId is null && member.ServicePackageVariantId is null
            ? null
            : await FindSelectableVariantAsync(model.ServicePackageVariantId, member.ServicePackageVariantId);
        var packageExists = model.ServicePackageVariantId is null
            ? member.ServicePackageVariantId is null
            : variant is not null;
        var trainerExists = model.AssignedTrainerId is null || await dbContext.Trainers
            .AnyAsync(item => item.Id == model.AssignedTrainerId &&
                (item.IsActive || item.AssignedMembers.Any(profile => profile.Id == id)));
        if (!packageExists)
            ModelState.AddModelError(nameof(model.ServicePackageVariantId), "Aktif bir üyelik, grup dersi veya Kids paketi seçmelisiniz.");
        if (!trainerExists) ModelState.AddModelError(nameof(model.AssignedTrainerId), "Aktif bir trainer seçmelisiniz.");

        model.FirstName = MemberLoginName.CleanPart(model.FirstName ?? string.Empty);
        model.LastName = MemberLoginName.CleanPart(model.LastName ?? string.Empty);
        model.NationalIdentityNumber = model.NationalIdentityNumber?.Trim() ?? string.Empty;
        var email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        var emailOwner = email is null ? null : await userManager.FindByEmailAsync(email);
        if (emailOwner is not null && emailOwner.Id != member.ApplicationUserId)
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi başka bir hesap tarafından kullanılıyor.");
        if (await dbContext.MemberProfiles.AnyAsync(profile =>
                profile.NationalIdentityNumber == model.NationalIdentityNumber && profile.Id != id))
            ModelState.AddModelError(nameof(model.NationalIdentityNumber), "Bu TC Kimlik No başka bir üyede kayıtlı.");

        var memberUserName = MemberLoginName.BuildUserName(model.FirstName, model.LastName);
        var userNameOwner = await userManager.FindByNameAsync(memberUserName);
        if (userNameOwner is not null && userNameOwner.Id != member.ApplicationUserId)
            ModelState.AddModelError(string.Empty, "Bu ad soyad ile daha önce bir üyelik oluşturulmuş.");

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

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync()
            : null;
        var user = member.ApplicationUser;
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = email;
        user.UserName = memberUserName;
        user.NormalizedEmail = email is null ? null : userManager.NormalizeEmail(email);
        user.NormalizedUserName = userManager.NormalizeName(memberUserName);
        user.EmailConfirmed = email is not null;
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        member.NationalIdentityNumber = model.NationalIdentityNumber;
        member.BirthDate = model.BirthDate;

        var packageChanged = member.ServicePackageVariantId != model.ServicePackageVariantId;
        if (variant is not null)
        {
            member.MembershipPackageId = await ResolveLegacyMembershipPackageIdAsync(variant);
            if (packageChanged)
            {
                member.RemainingClassCredits = MemberPackageEntitlement.CalculateInitialCredits(variant);
                member.LastMembershipOrderId = null;
                member.MembershipStartsAtUtc = DateTime.UtcNow;
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
        if (transaction is not null) await transaction.CommitAsync();
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

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync()
            : null;

        var orders = await dbContext.Orders
            .Where(item => item.MemberProfileId == id)
            .ToListAsync();
        foreach (var order in orders)
        {
            order.GuestEmail ??= member.ApplicationUser.Email;
            order.MemberProfileId = null;
        }

        var referredMembers = await dbContext.MemberProfiles
            .Where(item => item.ReferredByMemberProfileId == id)
            .ToListAsync();
        foreach (var referredMember in referredMembers)
        {
            referredMember.ReferredByMemberProfileId = null;
        }

        var personalTrainingSessions = await dbContext.PersonalTrainingSessions
            .Where(item => item.MemberProfileId == id)
            .ToListAsync();
        dbContext.PersonalTrainingSessions.RemoveRange(personalTrainingSessions);
        await dbContext.SaveChangesAsync();

        var result = await userManager.DeleteAsync(member.ApplicationUser);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(item => item.Description));
            return RedirectToAction(nameof(Index));
        }
        if (transaction is not null) await transaction.CommitAsync();
        TempData["StatusMessage"] = personalTrainingSessions.Count == 0
            ? "Üye profili ve giriş hesabı kalıcı olarak silindi."
            : $"Üye profili, giriş hesabı ve {personalTrainingSessions.Count} birebir ders kaydı kalıcı olarak silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTrainer(int memberProfileId, int? trainerId, int page = 1, string? query = null, string? state = null)
    {
        var member = await dbContext.MemberProfiles.FindAsync(memberProfileId);
        if (member is null) return NotFound();
        if (trainerId is not null && !await dbContext.Trainers.AnyAsync(item => item.Id == trainerId && item.IsActive))
            return BadRequest();

        member.AssignedTrainerId = trainerId;
        member.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        TempData["StatusMessage"] = trainerId is null ? "Antrenör ataması kaldırıldı." : "Antrenör üyeye atandı.";
        return RedirectToAction(nameof(Index), new { page, query, state });
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
        ViewBag.PackageOptions = await ManualPackageOptionsAsync(null);
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
                HasRelatedHistory = item.Orders.Any() || item.PersonalTrainingSessions.Any() || item.KitchenSubscriptions.Any()
            }).FirstOrDefaultAsync(); //AEO Silinemez Kuralı burada
    }

    private async Task<List<ManualMemberPackageOptionViewModel>> ManualPackageOptionsAsync(int? selectedId)
    {
        var variants = await dbContext.ServicePackageVariants.AsNoTracking()
            .Include(item => item.ServicePackage)
            .Where(item => ((item.IsActive && item.ServicePackage.IsActive) || item.Id == selectedId) &&
                (item.ServicePackage.Category == ServicePackageCategory.Membership ||
                 item.ServicePackage.Category == ServicePackageCategory.PersonalTraining ||
                 item.ServicePackage.Category == ServicePackageCategory.GroupClasses ||
                 item.ServicePackage.Category == ServicePackageCategory.KidsClub))
            .OrderBy(item => item.ServicePackage.Category)
            .ThenBy(item => item.ServicePackage.DisplayOrder)
            .ThenBy(item => item.DisplayOrder)
            .ToListAsync();

        return variants.Select(item =>
        {
            var presentation = ServicePackageCatalogService.Present(item);
            return new ManualMemberPackageOptionViewModel
            {
                Id = item.Id,
                PackageId = item.ServicePackageId,
                PackageName = item.ServicePackage.Name,
                VariantName = item.Name,
                Category = item.ServicePackage.Category.ToString(),
                GroupName = ServicePackageCatalogService.CategoryTitle(item.ServicePackage.Category),
                Price = presentation.Price,
                PriceNote = presentation.PriceNote,
                TotalPrice = item.BillingType == ServicePackageBillingType.MonthlySubscription &&
                    item.DurationMonths is > 0 && !item.PriceOnRequest
                    ? $"{((item.MonthlyPrice ?? 0) * item.DurationMonths.Value).ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))} ₺"
                    : null,
                Duration = MemberPackageEntitlement.DurationLabel(item),
                Rights = presentation.Rights,
                PaymentAmount = item.PriceOnRequest ? null : ManualOrderPaymentService.PackageTotal(item),
                PriceOnRequest = item.PriceOnRequest,
                Label = $"{item.ServicePackage.Name} — {item.Name} — {presentation.Price} ({presentation.PriceNote})"
            };
        }).ToList();
    }

    private async Task<ServicePackageVariant?> FindSelectableVariantAsync(int? variantId, int? currentVariantId = null)
    {
        if (variantId is null) return null;
        return await dbContext.ServicePackageVariants
            .Include(item => item.ServicePackage)
            .FirstOrDefaultAsync(item => item.Id == variantId &&
                ((item.IsActive && item.ServicePackage.IsActive) || item.Id == currentVariantId) &&
                (item.ServicePackage.Category == ServicePackageCategory.Membership ||
                 item.ServicePackage.Category == ServicePackageCategory.PersonalTraining ||
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
