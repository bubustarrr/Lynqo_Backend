using Lynqo_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LynqoBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
            var topUsers = await _context.UserXps
                .GroupBy(x => x.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalXp = g.Sum(x => x.XpAmount)
                })
                .OrderByDescending(x => x.TotalXp)
                .Take(50)
                .ToListAsync();

            var userIds = topUsers.Select(u => u.UserId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Username, u.DisplayName, u.ProfilePicUrl })
                .ToListAsync();

            var leaderboard = topUsers
                .Join(users,
                    xp => xp.UserId,
                    user => user.Id,
                    (xp, user) => new { user.Id, user.Username, user.DisplayName, user.ProfilePicUrl, xp.TotalXp })
                .OrderByDescending(x => x.TotalXp)
                .Select((item, index) => new
                {
                    Rank = index + 1,
                    item.Id,
                    Username = item.Username,                        // real username — for YOU badge comparison
                    DisplayName = item.DisplayName ?? item.Username, // display name — for showing in table
                    item.ProfilePicUrl,
                    Xp = item.TotalXp
                });

            return Ok(leaderboard);
        }

        // GET: api/leaderboard/weekly
        [HttpGet("weekly")]
        public async Task<IActionResult> GetWeeklyLeaderboard()
        {
            // Rolling Monday reset
            var diff = (7 + (int)DateTime.UtcNow.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            var startDate = DateTime.UtcNow.Date.AddDays(-diff);

            var weeklyStats = await _context.UserXps
                .Where(x => x.CreatedAt >= startDate)
                .GroupBy(x => x.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    WeeklyXp = g.Sum(x => x.XpAmount)
                })
                .OrderByDescending(x => x.WeeklyXp)
                .Take(50)
                .ToListAsync();

            var userIds = weeklyStats.Select(u => u.UserId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Username, u.DisplayName, u.ProfilePicUrl })
                .ToListAsync();

            var leaderboard = weeklyStats
                .Join(users,
                    xp => xp.UserId,
                    user => user.Id,
                    (xp, user) => new { user.Id, user.Username, user.DisplayName, user.ProfilePicUrl, xp.WeeklyXp })
                .OrderByDescending(x => x.WeeklyXp)
                .Select((item, index) => new
                {
                    Rank = index + 1,
                    item.Id,
                    Username = item.Username,
                    DisplayName = item.DisplayName ?? item.Username,
                    item.ProfilePicUrl,
                    Xp = item.WeeklyXp
                });

            return Ok(leaderboard);
        }
    }
}
