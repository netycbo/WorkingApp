using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Net;


namespace MyWorkingWebPage.Providers
{
    public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
    {
        public async Task<bool> SendEmailAsync(string fromName, string fromEmail, string message)
        {
            var smtpHost = config["Smtp:Host"];
            var smtpPort = int.Parse(config["Smtp:Port"]);
            var smtpUser = config["Smtp:User"];
            var smtpPass = config["Smtp:Pass"];
            var smtpFrom = config["Smtp:From"];

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(fromName, smtpFrom));
            email.To.Add(MailboxAddress.Parse(smtpFrom));
            email.Subject = $"Wiadomość od {fromName}";
            email.Body = new TextPart("plain") { Text = $"Od: {fromName} ({fromEmail})\n\n{message}" };

            using var smtp = new SmtpClient(new ProtocolLogger(Console.OpenStandardOutput())); // 👈 loguje całą sesję SMTP

            try
            {
                await smtp.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpUser, smtpPass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MailKit błąd SMTP");
                return false;
            }
        }
    }
}