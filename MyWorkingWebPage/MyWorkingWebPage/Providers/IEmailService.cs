
namespace MyWorkingWebPage.Providers
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string fromName, string fromEmail, string message);
    }
}