using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using NO23.Web.Domain.Entities;

namespace NO23.Web.Areas.Identity.Pages.Account;

public class ForgotPasswordModel(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);

        if (user is not null &&
            await userManager.IsEmailConfirmedAsync(user))
        {
            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new
                {
                    area = "Identity",
                    code
                },
                protocol: Request.Scheme);

            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                await emailSender.SendEmailAsync(
                    Input.Email,
                    "NO23 parola sıfırlama bağlantısı",
                    BuildModernPasswordResetEmail(callbackUrl));
            }
        }

        return RedirectToPage("./ForgotPasswordConfirmation");
    }

    private static string BuildModernPasswordResetEmail(
        string callbackUrl)
    {
        var encodedUrl = HtmlEncoder.Default.Encode(callbackUrl);

        return $"""
            <!doctype html>
            <html lang="tr">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>NO23 Parola Sıfırlama</title>
            </head>
            <body style="margin:0; padding:0; background:#f4f2ec; color:#171717; font-family:Arial, Helvetica, sans-serif;">
                <div style="display:none; max-height:0; overflow:hidden; opacity:0;">
                    NO23 hesabın için parola sıfırlama bağlantın hazır.
                </div>

                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f4f2ec; margin:0; padding:32px 16px;">
                    <tr>
                        <td align="center">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:640px; background:#ffffff; border-radius:18px; overflow:hidden; border:1px solid #e5e1d8;">
                                <tr>
                                    <td style="background:#090909; padding:28px 30px 26px; color:#ffffff;">
                                        <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                                            <tr>
                                                <td>
                                                    <div style="display:inline-block; padding:7px 11px; border:1px solid rgba(201,161,91,.38); border-radius:999px; color:#c9a15b; font-size:12px; font-weight:700; letter-spacing:1.8px;">
                                                        NO23
                                                    </div>
                                                </td>
                                                <td align="right" style="color:rgba(255,255,255,.5); font-size:12px; font-weight:700; letter-spacing:1.4px;">
                                                    MEMBER ACCESS
                                                </td>
                                            </tr>
                                        </table>

                                        <h1 style="margin:28px 0 10px; color:#ffffff; font-size:32px; line-height:1.12; font-weight:800;">
                                            Parolanı güvenle yenile.
                                        </h1>

                                        <p style="margin:0; max-width:520px; color:rgba(255,255,255,.68); font-size:15px; line-height:1.65;">
                                            NO23 Sports Club hesabına yeniden erişmek için aşağıdaki bağlantıyı kullanabilirsin.
                                        </p>
                                    </td>
                                </tr>

                                <tr>
                                    <td style="padding:32px 30px 30px;">
                                        <p style="margin:0 0 18px; color:#171717; font-size:16px; line-height:1.65;">
                                            Merhaba,
                                        </p>

                                        <p style="margin:0 0 26px; color:#4b4b47; font-size:15px; line-height:1.7;">
                                            Parola yenileme isteğini aldık. Bu bağlantı güvenliğin için sınırlı süre geçerlidir.
                                            İşleme devam etmek için butona tıkla.
                                        </p>

                                        <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 0 26px;">
                                            <tr>
                                                <td style="background:#c9a15b; border-radius:999px;">
                                                    <a href="{encodedUrl}" style="display:inline-block; padding:15px 24px; color:#090909; font-size:14px; font-weight:800; letter-spacing:.3px; text-decoration:none;">
                                                        Parolamı yenile
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <div style="padding:18px 20px; background:#f7f6f1; border:1px solid #e8e5dc; border-radius:14px;">
                                            <p style="margin:0 0 8px; color:#171717; font-size:13px; font-weight:800; letter-spacing:.4px;">
                                                Güvenlik notu
                                            </p>

                                            <p style="margin:0; color:#62625d; font-size:13px; line-height:1.65;">
                                                Bu işlemi sen başlatmadıysan bu e-postayı yok sayabilirsin. Parolan değiştirilmeyecek.
                                            </p>
                                        </div>

                                        <p style="margin:24px 0 0; color:#77776f; font-size:12px; line-height:1.7;">
                                            Buton çalışmazsa bu bağlantıyı tarayıcına kopyalayabilirsin:<br>
                                            <a href="{encodedUrl}" style="color:#c9a15b; word-break:break-all; text-decoration:underline;">{encodedUrl}</a>
                                        </p>
                                    </td>
                                </tr>

                                <tr>
                                    <td style="padding:20px 30px; background:#111410; color:rgba(255,255,255,.5); font-size:12px; line-height:1.6;">
                                        <strong style="display:block; margin-bottom:3px; color:#ffffff; font-size:13px;">NO23 Sports Club</strong>
                                        Train Better. Eat Better. Live Better.
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }

    private static string BuildPasswordResetEmail(
        string callbackUrl)
    {
        var encodedUrl = HtmlEncoder.Default.Encode(callbackUrl);

        return $"""
            <p>Merhaba,</p>
            <p>NO23 Sports Club hesabının parolasını yenilemek için aşağıdaki bağlantıyı kullanabilirsin.</p>
            <p><a href="{encodedUrl}">Parolamı yenile</a></p>
            <p>Bu işlemi sen başlatmadıysan bu e-postayı yok sayabilirsin.</p>
            """;
    }

    public class InputModel
    {
        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girmelisin.")]
        [Display(Name = "E-posta adresi")]
        public string Email { get; set; } = string.Empty;
    }
}
