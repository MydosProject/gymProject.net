using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;
using NO23.Web.Data.Seed;

namespace NO23.Web.Controllers;

[Authorize]
public class ProgressPhotosController(ApplicationDbContext db) : Controller
{
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Image(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole(ApplicationRoles.Admin);
        var isTrainer = User.IsInRole(ApplicationRoles.Trainer);
        var photo = await db.MemberProgressPhotos.AsNoTracking().Where(x => x.Id == id &&
            (isAdmin || x.MemberProfile.ApplicationUserId == userId ||
             (isTrainer && x.MemberProfile.AssignedTrainer != null && x.MemberProfile.AssignedTrainer.IsActive && x.MemberProfile.AssignedTrainer.ApplicationUserId == userId)))
            .FirstOrDefaultAsync();
        if (photo is null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(photo.Data, photo.ContentType);
    }
}
