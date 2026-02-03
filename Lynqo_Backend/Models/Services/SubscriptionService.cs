using Lynqo_Backend.Data;
using LynqoBackend.Models;
using LynqoBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;


namespace LynqoBackend.Models.Services
{
    public class SubscriptionService
    {
        private readonly LynqoDbContext _context;

        public SubscriptionService(LynqoDbContext context)
        {
            _context = context;
        }

        public async Task<SubscriptionDTO?> GetCurrentAsync(int userId)
        {
            var now = DateTime.UtcNow.Date;

            var sub = await _context.Subscriptions
                .Where(s => s.UserId == userId && (s.ExpiresAt == null || s.ExpiresAt >= now))
                .OrderByDescending(s => s.StartsAt)
                .FirstOrDefaultAsync();

            if (sub == null) return null;

            return new SubscriptionDTO
            {
                Id = sub.Id,
                PlanName = sub.PlanName,
                QuantityMonths = sub.QuantityMonths,
                StartsAt = sub.StartsAt,
                ExpiresAt = sub.ExpiresAt,
                AutoRenew = sub.AutoRenew
            };
        }

        public async Task<SubscriptionDTO> StartAsync(int userId, string planName, int quantityMonths)
        {
            if (quantityMonths <= 0) quantityMonths = 1;

            var start = DateTime.UtcNow.Date;
            var expires = start.AddMonths(quantityMonths);

            var sub = new Subscription
            {
                UserId = userId,
                PlanName = planName,
                QuantityMonths = quantityMonths,
                StartsAt = start,
                ExpiresAt = expires,
                AutoRenew = true,
                Provider = "internal"
            };

            _context.Subscriptions.Add(sub);

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
                user.IsPremium = true;

            await _context.SaveChangesAsync();

            return new SubscriptionDTO
            {
                Id = sub.Id,
                PlanName = sub.PlanName,
                QuantityMonths = sub.QuantityMonths,
                StartsAt = sub.StartsAt,
                ExpiresAt = sub.ExpiresAt,
                AutoRenew = sub.AutoRenew
            };
        }

        public async Task CancelAsync(int userId)
        {
            var now = DateTime.UtcNow.Date;

            var sub = await _context.Subscriptions
                .Where(s => s.UserId == userId && (s.ExpiresAt == null || s.ExpiresAt >= now))
                .OrderByDescending(s => s.StartsAt)
                .FirstOrDefaultAsync();

            if (sub == null) return;

            sub.AutoRenew = false;

            // Optionally downgrade immediately:
            // sub.ExpiresAt = now;
            // var user = await _context.Users.FindAsync(userId);
            // if (user != null) user.IsPremium = false;

            await _context.SaveChangesAsync();
        }
    }
}
