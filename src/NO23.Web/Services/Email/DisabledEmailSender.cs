using Microsoft.AspNetCore.Identity.UI.Services;

namespace NO23.Web.Services.Email;

public class DisabledEmailSender(
    ILogger<DisabledEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(
        string email,
        string subject,
        string htmlMessage)
    {
        logger.LogWarning(
            "SMTP mail gönderimi devre dışı veya eksik yapılandırılmış. Alıcı: {Email}, Konu: {Subject}",
            email,
            subject);

        return Task.CompletedTask;
    }
}
