using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Lynqo_Backend.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string verificationLink);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);

        // --- ÚJ METÓDUS: Általános e-mail küldő ---
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
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
                Subject = "Verify your email address - Lynqo",
                Body = $"Hi!\n\nPlease click the link below to verify your email address and complete your registration:\n{verificationLink}\n\nIf you did not register for an account, you can safely ignore this email.",
                IsBodyHtml = false
            };

            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
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
                Subject = "Password Reset - Lynqo",
                Body = $"Hi!\n\nWe received a request to reset your password. Click the link below to set a new password:\n{resetLink}\n\nIf you did not request a password reset, please ignore this email.\n\nThis link will expire in 1 hour.",
                IsBodyHtml = false
            };

            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }

        // --- ÚJ METÓDUS IMPLEMENTÁLÁSA ---
        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
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
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml // Engedélyezzük a HTML formázást (pl. <br> tagek a napi értesítőben)
            };

            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }
    }
}