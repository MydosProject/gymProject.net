using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using TrainerEntity = NO23.Web.Domain.Entities.Trainer;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class TrainersController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender) : Controller
{
    public async Task<IActionResult> Index()
    {
        var trainers = await dbContext.Trainers
            .AsNoTracking()
            .OrderBy(trainer => trainer.LastName)
            .ThenBy(trainer => trainer.FirstName)
            .Select(trainer => new TrainerListItemViewModel
            {
                Id = trainer.Id,
                FullName = trainer.FirstName + " " + trainer.LastName,
                Specialty = trainer.Specialty,
                Certifications = trainer.Certifications,
                ClassCount = trainer.GroupClasses.Count,
                IsActive = trainer.IsActive,
                HasPanelAccount = trainer.ApplicationUserId != null,
                HasPassword = trainer.ApplicationUser != null && trainer.ApplicationUser.PasswordHash != null
            })
            .ToListAsync();

        return View(trainers);
    }

    public IActionResult Create()
    {
        return View(new TrainerFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainerFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError(nameof(model.Email), "Giriş e-postası zorunludur.");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email!.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi zaten kullanılıyor.");
            return View(model);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim()
        };
        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }
        var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.Trainer);
        if (!roleResult.Succeeded)
        {
            AddIdentityErrors(roleResult);
            return View(model);
        }

        var trainer = MapToEntity(model);
        trainer.ApplicationUserId = user.Id;
        dbContext.Trainers.Add(trainer);
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        var invitationSent = await SendInvitationSafelyAsync(
            user, trainer.FirstName + " " + trainer.LastName);
        TempData["StatusMessage"] = invitationSent
            ? "PT şifresiz oluşturuldu ve şifre belirleme daveti gönderildi."
            : "PT şifresiz oluşturuldu; davet e-postası gönderilemedi.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var trainer = await dbContext.Trainers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (trainer is null)
        {
            return NotFound();
        }

        return View(MapToFormModel(trainer));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TrainerFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var trainer = await dbContext.Trainers.FindAsync(id);

        if (trainer is null)
        {
            return NotFound();
        }

        ApplyFormModel(trainer, model);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> CreatePanelAccount(int id)
    {
        var trainer = await dbContext.Trainers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (trainer is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(trainer.ApplicationUserId))
        {
            TempData["StatusMessage"] =
                "Bu eğitmenin zaten bir panel hesabı var.";

            return RedirectToAction(nameof(Index));
        }

        return View(new TrainerPanelAccountViewModel
        {
            TrainerId = trainer.Id,
            TrainerName = trainer.FirstName + " " + trainer.LastName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePanelAccount(
        TrainerPanelAccountViewModel model)
    {
        var trainer = await dbContext.Trainers
            .FirstOrDefaultAsync(item => item.Id == model.TrainerId);

        if (trainer is null)
        {
            return NotFound();
        }

        model.TrainerName =
            trainer.FirstName + " " + trainer.LastName;

        if (!string.IsNullOrWhiteSpace(trainer.ApplicationUserId))
        {
            ModelState.AddModelError(
                string.Empty,
                "Bu eğitmenin zaten bir panel hesabı var.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();

        var existingUser =
            await userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Bu e-posta adresi zaten kullanılıyor.");

            return View(model);
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync();

        var applicationUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = trainer.FirstName,
            LastName = trainer.LastName
        };

        var createResult =
            await userManager.CreateAsync(applicationUser);

        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }

        var roleResult =
            await userManager.AddToRoleAsync(
                applicationUser,
                ApplicationRoles.Trainer);

        if (!roleResult.Succeeded)
        {
            AddIdentityErrors(roleResult);
            return View(model);
        }

        trainer.ApplicationUserId = applicationUser.Id;
        trainer.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        var invitationSent = await SendInvitationSafelyAsync(
            applicationUser, model.TrainerName);

        TempData["StatusMessage"] =
            invitationSent
                ? $"{model.TrainerName} için şifresiz hesap oluşturuldu ve davet gönderildi."
                : $"{model.TrainerName} için hesap oluşturuldu; davet e-postası gönderilemedi.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendInvitation(int id)
    {
        var trainer = await dbContext.Trainers
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (trainer?.ApplicationUser is null) return NotFound();

        if (await userManager.HasPasswordAsync(trainer.ApplicationUser))
        {
            TempData["StatusMessage"] = "Bu PT şifresini daha önce oluşturmuş.";
            return RedirectToAction(nameof(Index));
        }

        var invitationSent = await SendInvitationSafelyAsync(
            trainer.ApplicationUser, trainer.FirstName + " " + trainer.LastName);
        TempData["StatusMessage"] = invitationSent
            ? "Şifre belirleme daveti yeniden gönderildi."
            : "Davet e-postası gönderilemedi; lütfen e-posta ayarlarını kontrol edin.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> SendInvitationSafelyAsync(ApplicationUser user, string trainerName)
    {
        try
        {
            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ResetPassword", null,
                new { area = "Identity", code, email = user.Email }, Request.Scheme);
            if (string.IsNullOrWhiteSpace(callbackUrl)) return false;

            var safeName = HtmlEncoder.Default.Encode(trainerName);
            var safeUrl = HtmlEncoder.Default.Encode(callbackUrl);
            await emailSender.SendEmailAsync(
                user.Email!,
                "NO23 Trainer hesabı — şifreni oluştur",
                $"""
                <p>Merhaba {safeName},</p>
                <p>NO23 Trainer panel hesabın oluşturuldu. İlk şifreni güvenli bağlantıdan kendin belirleyebilirsin.</p>
                <p><a href="{safeUrl}">Şifremi oluştur</a></p>
                <p>Bu bağlantı sınırlı süre geçerlidir. Süresi dolarsa yöneticiden yeni davet isteyebilirsin.</p>
                """);
            return true;
        }
        catch (Exception)
        {
            TempData["InvitationWarning"] =
                "Hesap oluşturuldu ancak davet e-postası gönderilemedi. Listeden daveti yeniden gönderebilirsiniz.";
            return false;
        }
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }
    }

    private static TrainerEntity MapToEntity(TrainerFormViewModel model)
    {
        var trainer = new TrainerEntity();

        ApplyFormModel(trainer, model);

        return trainer;
    }

    private static void ApplyFormModel(
        TrainerEntity trainer,
        TrainerFormViewModel model)
    {
        trainer.FirstName = model.FirstName.Trim();
        trainer.LastName = model.LastName.Trim();
        trainer.Specialty = model.Specialty.Trim();
        trainer.Certifications = model.Certifications?.Trim();
        trainer.Bio = model.Bio?.Trim();
        trainer.IsActive = model.IsActive;
        trainer.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static TrainerFormViewModel MapToFormModel(
        TrainerEntity trainer)
    {
        return new TrainerFormViewModel
        {
            Id = trainer.Id,
            FirstName = trainer.FirstName,
            LastName = trainer.LastName,
            Specialty = trainer.Specialty,
            Certifications = trainer.Certifications,
            Bio = trainer.Bio,
            IsActive = trainer.IsActive
        };
    }


}
