using System.Security.Claims;
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
public class MembershipPackageChangeRequestsController(
    ApplicationDbContext dbContext,
    MemberMembershipService membershipService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var rows = await dbContext.MembershipPackageChangeRequests
            .AsNoTracking()
            .OrderByDescending(request =>
                request.Status ==
                    MembershipPackageChangeRequestStatus.Pending)
            .ThenByDescending(request => request.RequestedAtUtc)
            .Select(request => new
            {
                request.Id,
                MemberName =
                    ((request.MemberProfile.ApplicationUser.FirstName ?? "") +
                     " " +
                     (request.MemberProfile.ApplicationUser.LastName ?? ""))
                        .Trim(),
                MemberEmail = request.MemberProfile.ApplicationUser.Email ?? "",
                CurrentPackageName = request.CurrentMembershipPackage.Name,
                RequestedPackageName = request.RequestedMembershipPackage.Name,
                request.Status,
                request.RequestedAtUtc,
                request.ResolvedAtUtc,
                request.AdminNote
            })
            .ToListAsync();

        var requests = rows
            .Select(request => new MembershipPackageChangeRequestListItemViewModel
            {
                Id = request.Id,
                MemberName = string.IsNullOrWhiteSpace(request.MemberName)
                    ? request.MemberEmail
                    : request.MemberName,
                MemberEmail = request.MemberEmail,
                CurrentPackageName = request.CurrentPackageName,
                RequestedPackageName = request.RequestedPackageName,
                Status = request.Status.GetDisplayName(),
                StatusCssClass = request.Status switch
                {
                    MembershipPackageChangeRequestStatus.Rejected or
                        MembershipPackageChangeRequestStatus.Cancelled =>
                        "is-cancelled",
                    _ => string.Empty
                },
                IsPending =
                    request.Status ==
                        MembershipPackageChangeRequestStatus.Pending,
                RequestedAtUtc = request.RequestedAtUtc,
                ResolvedAtUtc = request.ResolvedAtUtc,
                AdminNote = request.AdminNote
            })
            .ToList();

        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? adminNote)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return Challenge();
        }

        var result = await membershipService.ApprovePackageChangeRequestAsync(
            id,
            adminUserId,
            adminNote);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? "Paket talebi onaylandı."
                : result.ErrorMessage;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? adminNote)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return Challenge();
        }

        var result = await membershipService.RejectPackageChangeRequestAsync(
            id,
            adminUserId,
            adminNote);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? "Paket talebi reddedildi."
                : result.ErrorMessage;

        return RedirectToAction(nameof(Index));
    }
}
