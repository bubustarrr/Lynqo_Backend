using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Lynqo_Backend.Services
{
    public class BadgeService
    {
        private readonly LynqoDbContext _context;

        public BadgeService(LynqoDbContext context) => _context = context;

        public async Task EvaluateBadgesAsync(int userId)
        {
            // 1. Get the IDs of badges the user already owns
            var ownedBadgeIds = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BadgeId)
                .ToListAsync();

            var newBadges = new List<UserBadge>();

            // Helper to prevent adding duplicates in a single run
            void AwardBadgeIf(int badgeId, bool condition)
            {
                if (condition && !ownedBadgeIds.Contains(badgeId))
                {
                    newBadges.Add(new UserBadge { UserId = userId, BadgeId = badgeId, EarnedAt = DateTime.UtcNow });
                    ownedBadgeIds.Add(badgeId);
                }
            }

            // 2. Fetch User Stats
            var totalXp = await _context.UserXp.Where(x => x.UserId == userId).SumAsync(x => x.XpAmount);
            var lessonsCompletedCount = await _context.UserLessons.Where(ul => ul.UserId == userId).CountAsync();
            var friendsCount = await _context.Friendships.Where(f => (f.SenderId == userId || f.ReceiverId == userId) && f.Status == "accepted").CountAsync();
            var hasPerfectScore = await _context.UserLessons.AnyAsync(ul => ul.UserId == userId && ul.BestScore == 100);

            // 3. Evaluate Conditions (IDs match your SQL dump)
            AwardBadgeIf(1, lessonsCompletedCount >= 1); // Welcome!
            AwardBadgeIf(4, totalXp >= 500);             // XP Hunter
            AwardBadgeIf(5, totalXp >= 2000);            // XP Master
            AwardBadgeIf(6, friendsCount >= 3);          // Social Butterfly
            AwardBadgeIf(7, hasPerfectScore);            // Perfectionist

            // 4. Save new badges if earned
            if (newBadges.Any())
            {
                _context.UserBadges.AddRange(newBadges);
                await _context.SaveChangesAsync();
            }
        }
    }
}
