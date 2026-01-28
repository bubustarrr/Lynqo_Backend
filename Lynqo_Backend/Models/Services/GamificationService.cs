using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Lynqo_Backend.Models.Services
{
    public class GamificationService
    {
        private readonly LynqoDbContext _context;

        public GamificationService(LynqoDbContext context)
        {
            _context = context;
        }

        public async Task AddXpAsync(int userId, int xpAmount, string source = "lesson")
        {
            var xp = new UserXp { UserId = userId, XpAmount = xpAmount, Source = source };
            _context.UserXps.Add(xp);
            await _context.SaveChangesAsync();
        }

        public async Task<List<LeaderboardEntryDTO>> GetLeaderboardAsync(int leaderboardId)
        {
            return await _context.LeaderboardEntries
                .Where(le => le.LeaderboardId == leaderboardId)
                .Include(le => le.User)
                .OrderByDescending(le => le.Xp)
                .ThenBy(le => le.Rank)
                .Take(100)
                .Select(le => new LeaderboardEntryDTO
                {
                    UserId = le.UserId,
                    Username = le.User.Username,
                    DisplayName = le.User.DisplayName,
                    Xp = le.Xp,
                    Rank = le.Rank
                })
                .ToListAsync();
        }
    }
}
