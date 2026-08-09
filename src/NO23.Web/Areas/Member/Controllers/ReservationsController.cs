using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Enums;
using NO23.Web.Extensions;
using NO23.Web.Services;
using NO23.Web.ViewModels.Member;

namespace NO23.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize(Roles = ApplicationRoles.Member)]
public class ReservationsController(
    ApplicationDbContext dbContext,
    ClassReservationService reservationService,
    PersonalTrainingRequestService personalTrainingRequestService) : Controller
{
    public async Task<IActionResult> Index(int? trainerId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var memberContext = await dbContext.MemberProfiles
            .AsNoTracking()
            .Include(member => member.MembershipPackage)
            .Where(member => member.ApplicationUserId == userId)
            .Select(member => new
            {
                member.Id,
                member.MembershipPackage.IncludesPersonalTrainingSupport
            })
            .FirstOrDefaultAsync();
        var profileId = memberContext?.Id;

        var upcomingReservations = profileId is null
            ? []
            : await dbContext.ClassReservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.MemberProfileId == profileId &&
                    reservation.Status == ClassReservationStatus.Reserved &&
                    reservation.ClassSession.Status == ClassSessionStatus.Scheduled &&
                    reservation.ClassSession.GroupClass.IsActive &&
                    reservation.ClassSession.StartsAtUtc >= DateTime.UtcNow)
                .OrderBy(reservation => reservation.ClassSession.StartsAtUtc)
                .Select(reservation => new MemberReservationViewModel
                {
                    ReservationId = reservation.Id,
                    ClassName = reservation.ClassSession.GroupClass.Name,
                    TrainerName =
                        reservation.ClassSession.GroupClass.Trainer.FirstName + " " +
                        reservation.ClassSession.GroupClass.Trainer.LastName,
                    StartsAtUtc = reservation.ClassSession.StartsAtUtc
                })
                .ToListAsync();

        var availableSessions = await dbContext.ClassSessions
            .AsNoTracking()
            .Where(session =>
                session.Status == ClassSessionStatus.Scheduled &&
                session.StartsAtUtc >= DateTime.UtcNow &&
                session.GroupClass.IsActive)
            .OrderBy(session => session.StartsAtUtc)
            .Take(20)
            .Select(session => new AvailableClassSessionViewModel
            {
                SessionId = session.Id,
                ClassName = session.GroupClass.Name,
                TrainerName =
                    session.GroupClass.Trainer.FirstName + " " +
                    session.GroupClass.Trainer.LastName,
                StartsAtUtc = session.StartsAtUtc,
                DurationMinutes = session.GroupClass.DurationMinutes,
                DifficultyLevel = session.GroupClass.DifficultyLevel.ToString(),
                AverageCaloriesBurned = session.GroupClass.AverageCaloriesBurned,
                Capacity = session.CapacityOverride ?? session.GroupClass.Capacity,
                ReservedCount = session.Reservations.Count(reservation =>
                    reservation.Status == ClassReservationStatus.Reserved),
                IsReservedByMember = profileId != null &&
                    session.Reservations.Any(reservation =>
                        reservation.MemberProfileId == profileId &&
                        reservation.Status == ClassReservationStatus.Reserved)
            })
            .ToListAsync();

        var trainers = await dbContext.Trainers
            .AsNoTracking()
            .Where(trainer => trainer.IsActive)
            .OrderBy(trainer => trainer.FirstName)
            .ThenBy(trainer => trainer.LastName)
            .Select(trainer => new PersonalTrainerOptionViewModel
            {
                Id = trainer.Id,
                FullName = trainer.FirstName + " " + trainer.LastName,
                Specialty = trainer.Specialty,
                Bio = trainer.Bio
            })
            .ToListAsync();

        var selectedTrainerId = trainers.Any(trainer => trainer.Id == trainerId)
            ? trainerId
            : trainers.FirstOrDefault()?.Id;

        var personalTrainingRequests = new List<PersonalTrainingRequestListItemViewModel>();

        if (profileId is not null)
        {
            var personalTrainingRequestRows = await dbContext.PersonalTrainingRequests
                .AsNoTracking()
                .Where(request => request.MemberProfileId == profileId)
                .OrderByDescending(request =>
                    request.Status == PersonalTrainingRequestStatus.Pending)
                .ThenByDescending(request => request.CreatedAtUtc)
                .Select(request => new
                {
                    request.Id,
                    TrainerName = request.Trainer.FirstName + " " + request.Trainer.LastName,
                    request.PreferredDate,
                    request.PreferredTimeWindow,
                    request.Status,
                    request.TrainerNote,
                    request.ScheduledAtUtc,
                    request.AdminNote
                })
                .ToListAsync();

            personalTrainingRequests = personalTrainingRequestRows
                .Select(request => new PersonalTrainingRequestListItemViewModel
                {
                    Id = request.Id,
                    TrainerName = request.TrainerName,
                    PreferredDate = request.PreferredDate,
                    PreferredTimeWindow = request.PreferredTimeWindow,
                    Status = request.Status.GetDisplayName(),
                    ScheduledAtUtc = request.ScheduledAtUtc,
                    TrainerNote = request.TrainerNote,
                    AdminNote = request.AdminNote,
                    CanCancel =
                    request.Status == PersonalTrainingRequestStatus.Pending ||
                    (
                        request.Status == PersonalTrainingRequestStatus.Scheduled &&
                        request.ScheduledAtUtc.HasValue &&
                        request.ScheduledAtUtc.Value > DateTime.UtcNow
                    )
                })
                .ToList();
        }

        return View(new MemberReservationsIndexViewModel
        {
            UpcomingReservations = upcomingReservations,
            AvailableSessions = availableSessions,
            Trainers = trainers,
            SelectedTrainerId = selectedTrainerId,
            PersonalTrainingRequestInput = new PersonalTrainingRequestInputViewModel
            {
                TrainerId = selectedTrainerId ?? 0,
                PreferredDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                PreferredTimeWindow = PersonalTrainingRequestService.PreferredTimeWindows[0]
            },
            PreferredTimeWindows = PersonalTrainingRequestService.PreferredTimeWindows,
            PersonalTrainingRequests = personalTrainingRequests,
            CanRequestPersonalTraining =
                memberContext?.IncludesPersonalTrainingSupport == true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reserve(int classSessionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await reservationService.ReserveAsync(userId, classSessionId);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Rezervasyon oluşturuldu." : result.ErrorMessage;

        return LocalRedirect($"{Url.Action(nameof(Index))}#group-classes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int reservationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await reservationService.CancelAsync(userId, reservationId);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Rezervasyon iptal edildi." : result.ErrorMessage;

        return LocalRedirect($"{Url.Action(nameof(Index))}#upcoming-reservations");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPersonalTraining(
        PersonalTrainingRequestInputViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Birebir talep bilgilerini kontrol etmelisin.";
            return RedirectToPersonalTraining(model.TrainerId);
        }

        var result = await personalTrainingRequestService.CreateAsync(userId, model);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? "Birebir antrenman talebin eğitmene iletildi."
                : result.ErrorMessage;

        return RedirectToPersonalTraining(model.TrainerId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelPersonalTraining(int requestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await personalTrainingRequestService.CancelByMemberAsync(
            userId,
            requestId);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? "Birebir antrenman süreci iptal edildi."
                : result.ErrorMessage;

        return LocalRedirect($"{Url.Action(nameof(Index))}#personal-training");
    }

    private IActionResult RedirectToPersonalTraining(int trainerId)
    {
        return LocalRedirect(
            $"{Url.Action(nameof(Index), new { trainerId })}#personal-training");
    }
}
