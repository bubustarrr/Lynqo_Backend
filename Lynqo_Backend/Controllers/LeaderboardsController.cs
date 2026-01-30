using Lynqo_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lynqo_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaderboardController : ControllerBase
    {
        private readonly LynqoDbContext _context;

        public LeaderboardController(LynqoDbContext context)
        {
            _context = context;
        }

        // GET: api/leaderboard/global
        // Returns Top 50 Users by Total XP (All Time)
        [HttpGet("global")]
        public async Task<IActionResult> GetGlobalLeaderboard()
        {
            var leaderboard = await _context.UserXp
                .GroupBy(x => x.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalXp = g.Sum(x => x.XpAmount)
                })
                .OrderByDescending(x => x.TotalXp)
                .Take(50)
                .Join(_context.Users,
                      xp => xp.UserId,
                      user => user.Id,
                      (xp, user) => new
                      {
                          user.Id,
                          user.Username,
                          user.DisplayName,
                          user.ProfilePicUrl, // If you have avatars
                          xp.TotalXp
                      })
                .ToListAsync();

            // Add rank numbers (1, 2, 3...) in memory
            var rankedList = leaderboard.Select((item, index) => new
            {
                Rank = index + 1,
                item.Id,
                item.Username,
                item.DisplayName,
                item.ProfilePicUrl,
                item.TotalXp
            });

            return Ok(rankedList);
        }

        // GET: api/leaderboard/weekly
        // Returns Top 50 Users based on XP earned in the last 7 days
        [HttpGet("weekly")]
        public async Task<IActionResult> GetWeeklyLeaderboard()
        {
            // Calculate date 7 days ago
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var leaderboard = await _context.UserXp
                .Where(x => x.CreatedAt >= sevenDaysAgo) // Filter for recent XP only
                .GroupBy(x => x.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    WeeklyXp = g.Sum(x => x.XpAmount)
                })
                .OrderByDescending(x => x.WeeklyXp)
                .Take(50)
                .Join(_context.Users,
                      xp => xp.UserId,
                      user => user.Id,
                      (xp, user) => new
                      {
                          user.Id,
                          user.Username,
                          user.DisplayName,
                          user.ProfilePicUrl,
                          xp.WeeklyXp
                      })
                .ToListAsync();

            var rankedList = leaderboard.Select((item, index) => new
            {
                Rank = index + 1,
                item.Id,
                item.Username,
                item.DisplayName,
                item.ProfilePicUrl,
                TotalXp = item.WeeklyXp
            });

            return Ok(rankedList);
        }
    }
}
