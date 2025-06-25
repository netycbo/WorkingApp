using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Net;
using System.Text.Json;

namespace MyWorkingWebPage.Functions
{
    public class SendEmailFunction
    {
        private readonly ILogger<SendEmailFunction> _logger;
        private readonly IConfiguration _config;

        public SendEmailFunction(ILogger<SendEmailFunction> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [Function("SendEmail")]
        public async Task<HttpResponseData> SendEmail([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("SendEmail function processing request.");

            try
            {
                // Odczytaj dane z requestu
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var emailRequest = JsonSerializer.Deserialize<EmailRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (emailRequest == null)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid request data");
                    return badResponse;
                }

                bool success = await SendEmailAsync(emailRequest.FromName, emailRequest.FromEmail, emailRequest.Message);

                var response = req.CreateResponse(success ? System.Net.HttpStatusCode.OK : HttpStatusCode.InternalServerError);

                var result = new { success = success, message = success ? "Email sent successfully" : "Failed to send email" };
                await response.WriteAsJsonAsync(result);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SendEmail request");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Internal server error");
                return errorResponse;
            }
        }
        private async Task<bool> SendEmailAsync(string fromName, string fromEmail, string message)
        {
            var smtpHost = _config["Smtp:Host"];
            var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
            var smtpUser = _config["Smtp:User"];
            var smtpPass = _config["Smtp:Pass"];
            var smtpFrom = _config["Smtp:From"];

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(fromName, smtpFrom));
            email.To.Add(MailboxAddress.Parse(smtpFrom));
            email.Subject = $"Wiadomość od {fromName}";
            email.Body = new TextPart("plain") { Text = $"Od: {fromName} ({fromEmail})\n\n{message}" };

            using var smtp = new SmtpClient();

            try
            {
                await smtp.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpUser, smtpPass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {FromEmail}", fromEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MailKit błąd SMTP");
                return false;
            }
        }
    }

    public class EmailRequest
    {
        public string FromName { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}