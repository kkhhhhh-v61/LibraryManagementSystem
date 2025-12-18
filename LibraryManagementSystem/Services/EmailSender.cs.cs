using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
namespace LibraryManagementSystem.Services
{
    public class IEmailSender 
    {
        private readonly IConfiguration _config;

        public IEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            string mail = _config["EmailSettings:Mail"] ?? "";
            string displayName = _config["EmailSettings:DisplayName"] ?? "";
            string password = _config["EmailSettings:Password"] ?? "";
            string host = _config["EmailSettings:Host"] ?? "";

            int port = int.Parse(_config["EmailSettings:Port"] ?? "587");

            var mailMessage = new MailMessage
            {
                From = new MailAddress(mail, displayName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            using (var client = new SmtpClient(host, port)) 
            {
                client.Credentials = new NetworkCredential(mail, password);
                client.EnableSsl = true;

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}