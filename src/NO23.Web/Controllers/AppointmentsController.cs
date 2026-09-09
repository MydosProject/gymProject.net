using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels;

namespace NO23.Web.Controllers;

[AllowAnonymous]
[Route("randevu")]
public class AppointmentsController(ApplicationDbContext dbContext) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(new AppointmentRequestViewModel());

    [HttpPost(""), ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AppointmentRequestViewModel input)
    {
        if (!AppointmentRequestViewModel.Services.Contains(input.Service))
            ModelState.AddModelError(nameof(input.Service), "Geçerli bir hizmet seçmelisin.");
        var turkeyNow = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).DateTime;
        if (input.PreferredDate.HasValue && input.PreferredTime.HasValue &&
            input.PreferredDate.Value.ToDateTime(input.PreferredTime.Value) <= turkeyNow)
            ModelState.AddModelError(nameof(input.PreferredDate), "Gelecekte bir tarih ve saat seçmelisin.");
        if (!input.PreferredDate.HasValue || !input.PreferredTime.HasValue)
            ModelState.AddModelError(string.Empty, "Tarih ve saat seçmelisin.");
        if (!ModelState.IsValid) return View(input);

        var phone = input.PhoneNumber.Trim();
        var threshold = DateTime.UtcNow.AddMinutes(-5);
        var duplicate = await dbContext.AppointmentRequests.AnyAsync(x => x.PhoneNumber == phone &&
            x.Service == input.Service && x.PreferredDate == input.PreferredDate && x.PreferredTime == input.PreferredTime && x.CreatedAtUtc >= threshold);
        if (!duplicate)
        {
            dbContext.AppointmentRequests.Add(new AppointmentRequest
            {
                FullName = input.FullName.Trim(), PhoneNumber = phone, Service = input.Service,
                PreferredDate = input.PreferredDate!.Value, PreferredTime = input.PreferredTime!.Value,
                Notes = input.Notes?.Trim()
            });
            await dbContext.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Received));
    }

    [HttpGet("talep-alindi")]
    public IActionResult Received() => View();
}
