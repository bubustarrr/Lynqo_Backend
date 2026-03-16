using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lynqo_Backend.Services
{
    public interface IBadgeService
    {
        Task CheckAndAwardBadgesAsync(int userId);
    }

    public class BadgeService : IBadgeService
    {
        private readonly LynqoDbContext _context;

        public BadgeService(LynqoDbContext context)
        {
            _context = context;
        }

        public async Task CheckAndAwardBadgesAsync(int userId)
        {
            // 1. Fetch the User from the DB to get their current streak
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return; // Safety check: if user doesn't exist, do nothing
            }

            // Get the streak directly from the DB record
            int currentStreak = user.Streak; // Note: Change "Streak" if your model property is named differently (e.g. CurrentStreak)

            // 2. Fetch the user's currently earned badges so we don't give them duplicates
            var earnedBadgeIds = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BadgeId)
                .ToListAsync();

            // 3. Check if the user has a Rank of 1 in any Leaderboard
            bool hasWonWeeklyLeaderboard = await _context.LeaderboardEntries
                .AnyAsync(le => le.UserId == userId && le.Rank == 1);

            // 4. Fetch Total XP to check for XP badges
            var xpRecords = await _context.UserXps
                .Where(x => x.UserId == userId)
                .Select(x => x.XpAmount)
                .ToListAsync(); // Pull the numbers out of the DB first

            int totalXp = xpRecords.Sum(); // Then calculate the sum in C#


            // 5. EVALUATE BADGES 
            // Based on your database badges: 2 = 5-Day Streak, 3 = 10-Day Streak, 4 = 500 XP, 5 = 2000 XP, 8 = Champion

            // 5-Day Streak Badge
            if (currentStreak >= 5 && !earnedBadgeIds.Contains(2))
            {
                await AwardBadgeAsync(userId, 2);
            }

            // 10-Day Streak Badge
            if (currentStreak >= 10 && !earnedBadgeIds.Contains(3))
            {
                await AwardBadgeAsync(userId, 3);
            }

            // XP Hunter (500 XP)
            if (totalXp >= 500 && !earnedBadgeIds.Contains(4))
            {
                await AwardBadgeAsync(userId, 4);
            }

            // XP Master (2000 XP)
            if (totalXp >= 2000 && !earnedBadgeIds.Contains(5))
            {
                await AwardBadgeAsync(userId, 5);
            }

            // Weekly Champion Badge
            if (hasWonWeeklyLeaderboard && !earnedBadgeIds.Contains(8))
            {
                await AwardBadgeAsync(userId, 8);
            }

            // Save all newly awarded badges to the database
            await _context.SaveChangesAsync();
        }

        // Helper method to actually insert the badge
        private async Task AwardBadgeAsync(int userId, int badgeId)
        {
            var newBadge = new UserBadge
            {
                UserId = userId,
                BadgeId = badgeId,
                EarnedAt = DateTime.UtcNow
            };

            _context.UserBadges.Add(newBadge);
        }
    }
}
