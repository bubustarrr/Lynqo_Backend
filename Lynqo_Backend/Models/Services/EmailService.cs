using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Lynqo_Backend.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string verificationLink);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendVerificationEmailAsync(string toEmail, string verificationLink)
        {
            var smtpHost = _config["Email:SmtpHost"];
            var smtpPort = int.Parse(_config["Email:SmtpPort"]);
            var smtpUser = _config["Email:SmtpUser"];
            var smtpPass = _config["Email:SmtpPass"];

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUser, "Lynqo App"),
                Subject = "Erősítsd meg az email címedet - Lynqo",
                Body = $"Szia!\n\nKérlek kattints az alábbi linkre a regisztrációd megerősítéséhez:\n{verificationLink}\n\nHa nem te regisztráltál, hagyd figyelmen kívül ezt az üzenetet.",
                IsBodyHtml = false // Ezt később true-ra állíthatod, ha szép, dizájnos HTML levelet akarsz küldeni
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}