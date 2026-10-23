using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;
using NO23.Web.Domain.Entities;
using NO23.Web.ViewModels.TrainerPanel;

namespace NO23.Web.Areas.Trainer.Controllers;

[Area("Trainer"), Authorize(Roles = ApplicationRoles.Trainer)]
public class MembersController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? query, int page = 1)
    {
        var trainerId = await TrainerIdAsync();
        if (trainerId is null) return Forbid();
        query = query?.Trim();
        if (query?.Length > 80) query = query[..80];
        var members = AssignedMembers(trainerId.Value);
        if (!string.IsNullOrEmpty(query))
        {
            var term = query.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
            int.TryParse(query.TrimStart('#'), out var id);
            members = members.Where(member => member.Id == id ||
                (member.ApplicationUser.FirstName + " " + member.ApplicationUser.LastName)
                    .Replace("I", "ı").Replace("İ", "i").ToLower().Contains(term) ||
                (member.ApplicationUser.PhoneNumber != null && member.ApplicationUser.PhoneNumber.Contains(query)));
        }
        var count = await members.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(count / (double)TrainerMembersViewModel.PageSize)));
        return View(new TrainerMembersViewModel
        {
            Query = query, Page = page, TotalCount = count,
            Members = await members.OrderBy(member => member.ApplicationUser.FirstName)
                .ThenBy(member => member.ApplicationUser.LastName).ThenBy(member => member.Id)
                .Skip((page - 1) * TrainerMembersViewModel.PageSize).Take(TrainerMembersViewModel.PageSize).ToListAsync()
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var trainerId = await TrainerIdAsync();
        if (trainerId is null) return Forbid();
        var member = await AssignedMembers(trainerId.Value).FirstOrDefaultAsync(member => member.Id == id);
        return member is null ? NotFound() : View(member);
    }

    private IQueryable<MemberProfile> AssignedMembers(int trainerId) => db.MemberProfiles.AsNoTracking()
        .Include(member => member.ApplicationUser).Include(member => member.MembershipPackage)
        .Include(member => member.ServicePackageVariant).ThenInclude(variant => variant!.ServicePackage)
        .Where(member => member.AssignedTrainerId == trainerId);

    private Task<int?> TrainerIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return db.Trainers.Where(trainer => userId != null && trainer.ApplicationUserId == userId && trainer.IsActive)
            .Select(trainer => (int?)trainer.Id).FirstOrDefaultAsync();
    }
}
