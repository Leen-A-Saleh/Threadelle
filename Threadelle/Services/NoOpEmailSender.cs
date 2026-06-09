using Microsoft.AspNetCore.Identity.UI.Services;

namespace Threadelle.Services
{
    public class NoOpEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
            => Task.CompletedTask;
    }
}
