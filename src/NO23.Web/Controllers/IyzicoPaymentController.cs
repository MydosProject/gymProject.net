using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using NO23.Web.Services.Payments;

namespace NO23.Web.Controllers;

[AllowAnonymous]
[Route("payment/iyzico")]
public sealed class IyzicoPaymentController(
    IyzicoPaymentService paymentService,
    IWebHostEnvironment environment)
    : Controller
{
    [HttpPost("callback")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Callback(
        [FromForm] string? token,
        [FromQuery] string? returnUrl,
        CancellationToken cancellationToken)
    {
        var result =
            await paymentService.HandleCallbackAsync(
                token ?? string.Empty,
                cancellationToken);

        var paymentResult =
            result.Succeeded
                ? "success"
                : "failed";

        if (IsSafeReturnUrl(returnUrl))
        {
            var targetUrl =
                QueryHelpers.AddQueryString(
                    returnUrl!,
                    "payment",
                    paymentResult);

            return Redirect(targetUrl);
        }

        return RedirectToAction(
            "Index",
            "Orders",
            new
            {
                area = "Member",
                payment = paymentResult
            });
    }

    private bool IsSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) ||
            !Uri.TryCreate(
                returnUrl,
                UriKind.Absolute,
                out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var allowedPaths = new[]
        {
            "/Member/Orders",
            "/Shop/Confirmation",
            "/Kitchen/Confirmation"
        };

        var isAllowedPath =
            allowedPaths.Any(path =>
                uri.AbsolutePath.Equals(
                    path,
                    StringComparison.OrdinalIgnoreCase));

        if (!isAllowedPath)
        {
            return false;
        }

        // Uygulamanın çalıştığı aynı host'a dönüşe izin ver.
        var sameHost =
            string.Equals(
                uri.Host,
                Request.Host.Host,
                StringComparison.OrdinalIgnoreCase);

        // Development sırasında localhost/loopback dönüşlerine de izin ver.
        var localDevelopment =
            environment.IsDevelopment() &&
            uri.IsLoopback;

        return sameHost || localDevelopment;
    }
}