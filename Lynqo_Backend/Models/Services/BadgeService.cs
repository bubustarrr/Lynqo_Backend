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
            // 1. Fetch the User from the DB
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            int currentStreak = user.Streak;

            // 2. Fetch earned badges
            var earnedBadgeIds = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BadgeId)
                .ToListAsync();

            // 3. Gather data for various badges

            // Total XP (For Badges 4 & 5)
            var xpRecords = await _context.UserXps
                .Where(x => x.UserId == userId)
                .Select(x => x.XpAmount)
                .ToListAsync();
            int totalXp = xpRecords.Sum();

            // Has any completed lesson (For Badge 1)
            bool hasCompletedFirstLesson = await _context.UserLessons
                .AnyAsync(ul => ul.UserId == userId);

            // Friend Count (For Badge 6 - assuming accepted friendships count)
            int friendCount = await _context.Friendships
                .CountAsync(f => (f.SenderId == userId || f.ReceiverId == userId) && f.Status == "accepted");

            // Has a perfect score (For Badge 7)
            // Assuming 'BestScore' in UserLessons being 100 means 100% accuracy
            bool hasPerfectLesson = await _context.UserLessons
                .AnyAsync(ul => ul.UserId == userId && ul.BestScore == 100);

            // Has won weekly leaderboard (For Badge 8)
            bool hasWonWeeklyLeaderboard = await _context.LeaderboardEntries
                .AnyAsync(le => le.UserId == userId && le.Rank == 1);


            // 4. EVALUATE BADGES 

            // Badge 1: Welcome! (First lesson)
            if (hasCompletedFirstLesson && !earnedBadgeIds.Contains(1))
            {
                await AwardBadgeAsync(userId, 1);
            }

            // Badge 2: 5-Day Streak
            if (currentStreak >= 5 && !earnedBadgeIds.Contains(2))
            {
                await AwardBadgeAsync(userId, 2);
            }

            // Badge 3: 10-Day Streak
            if (currentStreak >= 10 && !earnedBadgeIds.Contains(3))
            {
                await AwardBadgeAsync(userId, 3);
            }

            // Badge 4: XP Hunter (500 XP)
            if (totalXp >= 500 && !earnedBadgeIds.Contains(4))
            {
                await AwardBadgeAsync(userId, 4);
            }

            // Badge 5: XP Master (2000 XP)
            if (totalXp >= 2000 && !earnedBadgeIds.Contains(5))
            {
                await AwardBadgeAsync(userId, 5);
            }

            // Badge 6: Social Butterfly (3 friends)
            if (friendCount >= 3 && !earnedBadgeIds.Contains(6))
            {
                await AwardBadgeAsync(userId, 6);
            }

            // Badge 7: Perfectionist (100% accuracy)
            if (hasPerfectLesson && !earnedBadgeIds.Contains(7))
            {
                await AwardBadgeAsync(userId, 7);
            }

            // Badge 8: Weekly Champion
            if (hasWonWeeklyLeaderboard && !earnedBadgeIds.Contains(8))
            {
                await AwardBadgeAsync(userId, 8);
            }

            // 5. Save all newly awarded badges
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
