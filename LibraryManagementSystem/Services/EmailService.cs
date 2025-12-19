using System.Net;
using System.Net.Mail;

#nullable disable warnings

namespace LibraryManagementSystem.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var mailSettings = _configuration.GetSection("MailSettings");

            string mailServer = mailSettings["Server"];
            int port = int.Parse(mailSettings["Port"]);
            string senderEmail = mailSettings["SenderEmail"];
            string password = mailSettings["Password"];

            string displayName = mailSettings["DisplayName"] ?? "Library Reminder Services";

            var client = new SmtpClient(mailServer, port)
            {
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage(senderEmail, toEmail, subject, message)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mailMessage);
        }
    }
}