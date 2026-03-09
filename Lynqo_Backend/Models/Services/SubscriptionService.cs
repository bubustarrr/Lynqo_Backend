using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using LynqoBackend.Models;
using LynqoBackend.Models.DTOs;

namespace LynqoBackend.Models.Services
{
    public class SubscriptionService
    {
        private readonly LynqoDbContext _context;

        public SubscriptionService(LynqoDbContext context)
        {
            _context = context;
        }

        // 1. A jelenlegi aktív elõfizetés lekérése
        public async Task<SubscriptionDTO?> GetCurrentAsync(int userId)
        {
            var activeSubscription = await _context.Subscriptions
                .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.ExpiresAt)
                .FirstOrDefaultAsync();

            if (activeSubscription == null)
            {
                return null;
            }

            // DTO-ba csomagoljuk a választ a frontend számára
            return new SubscriptionDTO
            {
                Id = activeSubscription.Id,
                PlanName = activeSubscription.PlanName ?? "basic",
                QuantityMonths = activeSubscription.QuantityMonths,
                StartsAt = activeSubscription.StartsAt,
                ExpiresAt = activeSubscription.ExpiresAt,
                AutoRenew = activeSubscription.AutoRenew
            };
        }

        // 2. Új elõfizetés indítása (Itt van a 4 paraméter!)
        public async Task<SubscriptionDTO> StartAsync(int userId, string planName, int quantityMonths, bool autoRenew)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found in database.");
            }

            // Létrehozzuk az új elõfizetés rekordot
            var newSubscription = new Subscription
            {
                UserId = userId,
                PlanName = planName,
                QuantityMonths = quantityMonths,
                AutoRenew = autoRenew, // Itt mentjük a frontendrõl jövõ checkbox értékét
                StartsAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMonths(quantityMonths),
                Provider = "System", // Késõbb ez lehet pl. "Stripe" vagy "PayPal"
                TransactionId = Guid.NewGuid().ToString() // Generálunk egy egyedi azonosítót a tranzakciónak
            };

            _context.Subscriptions.Add(newSubscription);

            // !! FONTOS !! Beállítjuk a felhasználót prémium taggá
            user.IsPremium = true;

            // Mentsük el a változásokat az adatbázisba
            await _context.SaveChangesAsync();

            // Visszaadjuk a mentett adatokat DTO formájában
            return new SubscriptionDTO
            {
                Id = newSubscription.Id,
                PlanName = newSubscription.PlanName,
                QuantityMonths = newSubscription.QuantityMonths,
                StartsAt = newSubscription.StartsAt,
                ExpiresAt = newSubscription.ExpiresAt,
                AutoRenew = newSubscription.AutoRenew
            };
        }

        // 3. Elõfizetés lemondása (Auto-renew kikapcsolása)
        public async Task<bool> CancelAsync(int userId)
        {
            var activeSubscription = await _context.Subscriptions
                .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow && s.AutoRenew)
                .OrderByDescending(s => s.ExpiresAt)
                .FirstOrDefaultAsync();

            if (activeSubscription != null)
            {
                // Nem töröljük az elõfizetést (hiszen már kifizette), csak a megújulást kapcsoljuk ki
                activeSubscription.AutoRenew = false;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}