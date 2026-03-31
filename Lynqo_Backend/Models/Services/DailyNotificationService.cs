using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Lynqo_Backend.Data;

namespace Lynqo_Backend.Services
{
    public class DailyNotificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DailyNotificationService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Kiszámoljuk a következő delet (12:00)
                var now = DateTime.Now;
                var nextNoon = now.Date.AddHours(12);

                if (now >= nextNoon)
                {
                    nextNoon = nextNoon.AddDays(1);
                }

                var delay = nextNoon - now;

                // ---------------------------------------------------------------------------------
                // TESZTELÉSHEZ: 
                //
                delay = TimeSpan.FromSeconds(20); 
                // ---------------------------------------------------------------------------------

                Console.WriteLine($"\n[Napi Értesítő] A következő e-mailek küldése ekkor lesz: {nextNoon}");
                Console.WriteLine($"[Napi Értesítő] Várakozás: {delay.TotalHours:F2} óra...\n");

                await Task.Delay(delay, stoppingToken);

                await SendDailyEmailsAsync();
            }
        }

        private async Task SendDailyEmailsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LynqoDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            Console.WriteLine("[Napi Értesítő] E-mailek küldésének megkezdése...");

            try
            {
                // 1. Csak azokat a felhasználókat keressük, akik kérték az értesítést a Settings-ben
                var usersToNotify = await (from u in dbContext.Users
                                           join s in dbContext.Settings on u.Id equals s.UserId
                                           where s.NotificationsEnabled == true
                                           select u).ToListAsync();

                if (!usersToNotify.Any())
                {
                    Console.WriteLine("[Napi Értesítő] Nincs olyan felhasználó, aki értesítést kért.");
                    return;
                }

                // 2. AppUrl beolvasása a konfigból
                var baseUrl = config["AppUrl"] ?? "https://localhost:7118";

                // 3. HTML SABLON BEOLVASÁSA
                // Ellenőrizzük, hogy létezik-e az EmailTemplates mappa és benne az email.html
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "email.html");
                if (!File.Exists(templatePath))
                {
                    Console.WriteLine($"[Napi Értesítő Hiba]: Nem található a HTML sablon ezen a helyen: {templatePath}");
                    return;
                }

                var htmlTemplate = await File.ReadAllTextAsync(templatePath);

                // 4. E-mail küldő (SMTP) beállítása
                var smtpHost = config["Email:SmtpHost"];
                var portStr = config["Email:SmtpPort"];
                var smtpPort = string.IsNullOrEmpty(portStr) ? 587 : int.Parse(portStr);
                var smtpUser = config["Email:SmtpUser"];
                var smtpPass = config["Email:SmtpPass"];

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                // 5. Ciklus: Végigmegyünk a felhasználókon és egyesével elküldjük
                foreach (var user in usersToNotify)
                {
                    // Kicseréljük a {{Username}} és a {{BaseUrl}} szövegeket a HTML-ben
                    var personalizedHtml = htmlTemplate
                        .Replace("{{Username}}", user.Username ?? "Diák")
                        .Replace("{{BaseUrl}}", baseUrl);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpUser ?? "lynqo.support@gmail.com", "Lynqo App"),
                        Subject = "Level up your study this week! ~ Reminder°˙",
                        Body = personalizedHtml,
                        IsBodyHtml = true // Ez garantálja, hogy dizájnosan (HTML-ként) jelenik meg
                    };

                    mailMessage.To.Add(user.Email);

                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine($"[Napi Értesítő] E-mail kiküldve (Canva dizájnnal): {user.Email}");
                }

                Console.WriteLine($"[Napi Értesítő] Sikeresen kiküldve {usersToNotify.Count} felhasználónak.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Napi Értesítő Hiba]: {ex.Message}");
            }
        }
    }
}