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

        // Sadece Siparişlerim sayfasına dönüşe izin veriyoruz.
        if (!uri.AbsolutePath.Equals(
                "/Member/Orders",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Ngrok üzerinden başlanmışsa callback host'u ile aynı olur.
        var sameHost =
            string.Equals(
                uri.Host,
                Request.Host.Host,
                StringComparison.OrdinalIgnoreCase);

        // Development sırasında localhost'a dönüşe de izin ver.
        var localDevelopment =
            environment.IsDevelopment() &&
            uri.IsLoopback;

        return sameHost || localDevelopment;
    }
}