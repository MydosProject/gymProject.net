using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NO23.Web.Data;

namespace NO23.Web.Controllers;

[AllowAnonymous]
public class MediaFilesController(ApplicationDbContext dbContext) : Controller
{
    // Serves admin-uploaded photos. An upload never changes, so browsers may cache it for a year.
    [HttpGet("media/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var image = await dbContext.UploadedImages
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.ContentType, item.Data })
            .FirstOrDefaultAsync(cancellationToken);

        if (image is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(image.Data, image.ContentType);
    }
}
