using Microsoft.AspNetCore.Identity.UI.Services;

namespace NO23.Web.Services.Email;

public class DevelopmentEmailSender(
    ILogger<DevelopmentEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(
        string email,
        string subject,
        string htmlMessage)
    {
        logger.LogWarning(
            "SMTP ayarı bulunmadığı için e-posta gönderilmedi. Development reset içeriği. Alıcı: {Email}, Konu: {Subject}, İçerik: {HtmlMessage}",
            email,
            subject,
            htmlMessage);

        return Task.CompletedTask;
    }
}
