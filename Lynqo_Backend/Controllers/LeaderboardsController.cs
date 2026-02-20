using Lynqo_Backend.Data;
using Microsoft.AspNetCore.Authorization; // Add [Authorize] if you want it private
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
        [HttpGet("global")]
        public async Task<IActionResult> GetGlobalLeaderboard()
        {
            // 1. Get Top 50 Users based on Total XP
            var topUsers = await _context.UserXp // Use UserXps if plural
                .GroupBy(x => x.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalXp = g.Sum(x => x.XpAmount)
                })
                .OrderByDescending(x => x.TotalXp)
                .Take(50)
                .ToListAsync(); // Execute SQL here

            // 2. Get User Details for those IDs
            var userIds = topUsers.Select(u => u.UserId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Username, u.DisplayName, u.ProfilePicUrl })
                .ToListAsync();

            // 3. Combine in Memory (User + XP)
            var leaderboard = topUsers.Join(users,
                xp => xp.UserId,
                user => user.Id,
                (xp, user) => new
                {
                    user.Id,
                    user.Username,
                    user.DisplayName,
                    user.ProfilePicUrl,
                    xp.TotalXp
                })
                .OrderByDescending(x => x.TotalXp) // Ensure order again
                .Select((item, index) => new
                {
                    Rank = index + 1,
                    item.Id,
                    Username = item.DisplayName ?? item.Username, // Fallback to username
                    item.ProfilePicUrl,
                    Xp = item.TotalXp
                });

            return Ok(leaderboard);
        }

        // GET: api/leaderboard/weekly
        [HttpGet("weekly")]
        public async Task<IActionResult> GetWeeklyLeaderboard()
        {
            // Option A: Rolling 7 Days (Last 7 days from right now)
            var startDate = DateTime.UtcNow.AddDays(-7);

            // Option B: Reset on Monday (Uncomment if you want this)
            // var diff = (7 + (DateTime.UtcNow.DayOfWeek - DayOfWeek.Monday)) % 7;
            // var startDate = DateTime.UtcNow.Date.AddDays(-1 * diff); 

            // 1. Get Weekly XP Sums
            var weeklyStats = await _context.UserXp // Use UserXps if plural
                .Where(x => x.CreatedAt >= startDate)
                .GroupBy(x => x.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    WeeklyXp = g.Sum(x => x.XpAmount)
                })
                .OrderByDescending(x => x.WeeklyXp)
                .Take(50)
                .ToListAsync(); // Execute SQL

            // 2. Get User Details
            var userIds = weeklyStats.Select(u => u.UserId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Username, u.DisplayName, u.ProfilePicUrl })
                .ToListAsync();

            // 3. Combine
            var leaderboard = weeklyStats.Join(users,
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
                .OrderByDescending(x => x.WeeklyXp)
                .Select((item, index) => new
                {
                    Rank = index + 1,
                    item.Id,
                    Username = item.DisplayName ?? item.Username,
                    item.ProfilePicUrl,
                    Xp = item.WeeklyXp
                });

            return Ok(leaderboard);
        }
    }
}
