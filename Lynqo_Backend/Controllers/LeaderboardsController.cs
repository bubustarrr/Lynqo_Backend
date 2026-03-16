using Lynqo_Backend.Data; // Adjust if your DbContext is in Models
using Lynqo_Backend.Models;
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

        // Exactly the leagues you requested, in order from bottom to top!
        private static readonly string[] LeagueOrder = { "Bronze", "Copper", "Silver", "Gold", "Emerald", "Obsidian", "Diamond" };

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
                .Select(g => new { UserId = g.Key, TotalXp = g.Sum(x => x.XpAmount) })
                .OrderByDescending(x => x.TotalXp)
                .Take(50)
                .ToListAsync();

            var userIds = topUsers.Select(u => u.UserId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Username, u.DisplayName, u.ProfilePicUrl, u.League })
                .ToDictionaryAsync(u => u.Id);

            var leaderboard = topUsers.Select((item, index) => new
            {
                Rank = index + 1,
                item.UserId,
                Username = users[item.UserId].Username,
                DisplayName = users[item.UserId].DisplayName ?? users[item.UserId].Username,
                ProfilePicUrl = users[item.UserId].ProfilePicUrl,
                League = users[item.UserId].League,
                Xp = item.TotalXp
            });

            return Ok(leaderboard);
        }

        // GET: api/leaderboard/weekly
        [HttpGet("weekly")]
        public async Task<IActionResult> GetWeeklyLeaderboard([FromQuery] string? league = null)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            // 1. Calculate the start of the current week (Rolling Monday)
            var diff = (7 + (int)DateTime.UtcNow.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            var currentWeekStart = DateTime.UtcNow.Date.AddDays(-diff);

            // 2. Automatically finalize last week
            await ProcessWeeklyResetAsync(currentWeekStart);

            // 3. Fetch the current user's actual assigned league
            var userLeague = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.League)
                .FirstOrDefaultAsync() ?? "Bronze";

            // 4. CRITICAL FIX: If the frontend sends ?league=Gold, use it! Otherwise fallback to the user's league.
            var targetLeague = !string.IsNullOrEmpty(league) ? league : userLeague;

            // 5. Fetch users specifically in the targetLeague
            var usersInLeague = await _context.Users
                .Where(u => u.League == targetLeague)
                .Select(u => new { u.Id, u.Username, u.DisplayName, u.ProfilePicUrl })
                .ToListAsync();

            var leagueUserIds = usersInLeague.Select(u => u.Id).ToList();

            // Fetch ONLY the XP for users in this specific league
            var weeklyStats = await _context.UserXps
                .Where(x => x.CreatedAt >= currentWeekStart && leagueUserIds.Contains(x.UserId))
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key, WeeklyXp = g.Sum(x => x.XpAmount) })
                .ToDictionaryAsync(x => x.UserId, x => x.WeeklyXp);

            // 6. Build and sort the leaderboard
            var leaderboard = usersInLeague
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    DisplayName = u.DisplayName ?? u.Username,
                    u.ProfilePicUrl,
                    Xp = weeklyStats.ContainsKey(u.Id) ? weeklyStats[u.Id] : 0
                })
                .OrderByDescending(x => x.Xp)
                .Select((item, index) => new
                {
                    Rank = index + 1,
                    item.Id,
                    item.Username,
                    item.DisplayName,
                    item.ProfilePicUrl,
                    item.Xp,
                    League = targetLeague, // Return the requested league name back
                    Zone = GetZone(index + 1, usersInLeague.Count, targetLeague)
                });

            return Ok(new
            {
                League = targetLeague, // Confirms to the frontend which league this actually is
                EndsAt = currentWeekStart.AddDays(7),
                Leaderboard = leaderboard
            });
        }


        // --- HELPER METHODS ---

        private async Task ProcessWeeklyResetAsync(DateTime currentWeekStart)
        {
            var lastWeekStart = currentWeekStart.AddDays(-7);
            var lastWeekEnd = currentWeekStart.AddTicks(-1); // Sunday 11:59:59 PM

            // Have we already saved last week's leaderboards?
            bool alreadyProcessed = await _context.Leaderboards.AnyAsync(l => l.StartDate == lastWeekStart);
            if (alreadyProcessed) return;

            // Process every league tier
            foreach (var league in LeagueOrder)
            {
                var usersInLeague = await _context.Users.Where(u => u.League == league).ToListAsync();
                if (!usersInLeague.Any()) continue;

                var userIds = usersInLeague.Select(u => u.Id).ToList();
                var lastWeekXp = await _context.UserXps
                    .Where(x => x.CreatedAt >= lastWeekStart && x.CreatedAt <= lastWeekEnd && userIds.Contains(x.UserId))
                    .GroupBy(x => x.UserId)
                    .Select(g => new { UserId = g.Key, Xp = g.Sum(x => x.XpAmount) })
                    .ToDictionaryAsync(x => x.UserId, x => x.Xp);

                // Sort players from last week
                var rankedUsers = usersInLeague
                    .Select(u => new { User = u, Xp = lastWeekXp.ContainsKey(u.Id) ? lastWeekXp[u.Id] : 0 })
                    .OrderByDescending(x => x.Xp)
                    .ToList();

                // Save historical Leaderboard
                var board = new Leaderboard { LeagueName = league, StartDate = lastWeekStart, EndDate = lastWeekEnd };
                _context.Leaderboards.Add(board);
                await _context.SaveChangesAsync(); // Get the new board.Id

                int rank = 1;
                int totalPlayers = rankedUsers.Count;
                int currentLeagueIndex = Array.IndexOf(LeagueOrder, league);

                foreach (var ru in rankedUsers)
                {
                    // 1. Permanently Save their historical finish
                    _context.LeaderboardEntries.Add(new LeaderboardEntry
                    {
                        LeaderboardId = board.Id,
                        UserId = ru.User.Id,
                        Xp = ru.Xp,
                        Rank = rank
                    });

                    // 2. PROMOTION / DEMOTION LOGIC
                    if (rank <= 5)
                    {
                        // Promote to next league (if not currently in Diamond)
                        if (currentLeagueIndex < LeagueOrder.Length - 1)
                            ru.User.League = LeagueOrder[currentLeagueIndex + 1];
                    }
                    else if (rank >= Math.Max(6, totalPlayers - 4))
                    {
                        // Demote to lower league (if not currently in Bronze)
                        if (currentLeagueIndex > 0)
                            ru.User.League = LeagueOrder[currentLeagueIndex - 1];
                    }

                    rank++;
                }
            }

            // Save all new leagues and history entries to the database!
            await _context.SaveChangesAsync();
        }

        private string GetZone(int rank, int totalPlayers, string currentLeague)
        {
            // Top 5 promote. Bottom 5 demote (ensure we don't overlap if there are less than 10 players).
            if (rank <= 5) return currentLeague == "Diamond" ? "Champion" : "Promotion";
            if (rank >= Math.Max(6, totalPlayers - 4)) return currentLeague == "Bronze" ? "Safe" : "Demotion";
            return "Safe";
        }
    }
}
