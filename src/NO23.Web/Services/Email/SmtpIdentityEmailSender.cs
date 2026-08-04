using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace NO23.Web.Services.Email;

public class SmtpIdentityEmailSender(
    IOptions<SmtpEmailOptions> options,
    ILogger<SmtpIdentityEmailSender> logger) : IEmailSender
{
    private readonly SmtpEmailOptions options = options.Value;

    public async Task SendEmailAsync(
        string email,
        string subject,
        string htmlMessage)
    {
        if (!HasRequiredSettings())
        {
            logger.LogWarning(
                "SMTP mail gönderimi için gerekli ayarlar eksik. Alıcı: {Email}",
                email);

            return;
        }

        var fromAddress = options.FromAddress!;
        var userName = options.UserName!;
        var password = options.Password!;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            options.FromName,
            fromAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlMessage
        }.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = options.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await client.ConnectAsync(
            options.Host,
            options.Port,
            socketOptions);
        await client.AuthenticateAsync(
            userName,
            password);
        await client.SendAsync(message);
        await client.DisconnectAsync(quit: true);

        logger.LogInformation(
            "Parola sıfırlama e-postası gönderildi. Alıcı: {Email}",
            email);
    }

    private bool HasRequiredSettings()
    {
        return options.Enabled
            && !string.IsNullOrWhiteSpace(options.Host)
            && options.Port > 0
            && !string.IsNullOrWhiteSpace(options.UserName)
            && !string.IsNullOrWhiteSpace(options.Password)
            && !string.IsNullOrWhiteSpace(options.FromAddress);
    }
}
