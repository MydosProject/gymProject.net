using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NO23.Web.Data.Seed;
using NO23.Web.Services;

namespace NO23.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = ApplicationRoles.Admin)]
public class MediaController(UploadedImageService images) : Controller
{
    // Called by the admin forms' "upload from computer" control; returns the link
    // that the form then saves in its image field.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(UploadedImageService.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadedImageService.MaxRequestBytes)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "Fotoğraf seçilmedi." });
        }

        if (file.Length > UploadedImageService.MaxBytes)
        {
            return BadRequest(new { error = $"Fotoğraf en fazla {UploadedImageService.MaxBytesText} olabilir." });
        }

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);
        var result = await images.SaveAsync(buffer.ToArray(), User.Identity?.Name, cancellationToken);

        return result.Succeeded
            ? Json(new { url = result.Url })
            : BadRequest(new { error = result.Error });
    }
}
