using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lynqo_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly LynqoDbContext _context;

        public UserController(LynqoDbContext context)
        {
            _context = context;
        }

        // GET: api/user/me?courseId=1
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile([FromQuery] int? courseId)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound();

            // Optional: keep this only if HeartService exists in your project
            HeartService.ApplyHeartRefill(user);
            await _context.SaveChangesAsync();

            // Global lifetime XP from userxp table
            int globalXp = await _context.UserXps
                .Where(x => x.UserId == userId)
                .SumAsync(x => (int?)x.XpAmount) ?? 0;

            // Course XP from userlessons + lessons
            int currentCourseXp = globalXp;
            if (courseId.HasValue)
            {
                currentCourseXp = await _context.UserLessons
                    .Where(ul => ul.UserId == userId)
                    .Join(
                        _context.Lessons.Where(l => l.CourseId == courseId.Value),
                        ul => ul.LessonId,
                        l => l.Id,
                        (ul, l) => (int?)ul.XpEarned
                    )
                    .SumAsync() ?? 0;
            }

            var activityDates = await _context.UserXps
                .Where(x => x.UserId == userId)
                .Select(x => x.CreatedAt.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();

            int currentStreak = CalculateStreak(activityDates);

            return Ok(new
            {
                user.Id,
                user.Username,
                user.DisplayName,
                user.ProfilePicUrl,
                user.Hearts,
                user.Coins,
                user.IsPremium,
                TotalXp = currentCourseXp,
                LifetimeXp = globalXp,
                Streak = currentStreak
            });
        }

        // GET: api/user/me/resources
        [HttpGet("me/resources")]
        [Authorize]
        public async Task<IActionResult> GetMyResources()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound();

            HeartService.ApplyHeartRefill(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.Username,
                hearts = user.Hearts,
                coins = user.Coins
            });
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetUser(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Username,
                user.DisplayName,
                user.Email,
                user.Hearts,
                user.Coins,
                user.IsPremium,
                user.Role,
                user.CreatedAt
            });
        }

        [HttpPost("{id}/xp")]
        public async Task<IActionResult> AddXp(int id, [FromBody] AddXpRequest request)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == id);
            if (!userExists)
                return NotFound(new { message = "User not found." });

            var newXp = new UserXp
            {
                UserId = id,
                XpAmount = request.XpAmount,
                Source = request.Source,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserXps.Add(newXp);
            await _context.SaveChangesAsync();

            int totalXp = await _context.UserXps
                .Where(xp => xp.UserId == id)
                .SumAsync(xp => (int?)xp.XpAmount) ?? 0;

            return Ok(new { totalXp, message = "XP added!" });
        }

        [HttpGet("{id}/xp")]
        public async Task<IActionResult> GetUserXp(int id)
        {
            int totalXp = await _context.UserXps
                .Where(xp => xp.UserId == id)
                .SumAsync(xp => (int?)xp.XpAmount) ?? 0;

            var recent = await _context.UserXps
                .Where(xp => xp.UserId == id)
                .OrderByDescending(xp => xp.CreatedAt)
                .Take(10)
                .Select(xp => new UserXpEntryDTO
                {
                    XpAmount = xp.XpAmount,
                    Source = xp.Source,
                    CreatedAt = xp.CreatedAt
                })
                .ToListAsync();

            return Ok(new UserXpDTO
            {
                TotalXp = totalXp,
                RecentXp = recent
            });
        }

        [HttpPost("spend-heart")]
        [Authorize]
        public async Task<IActionResult> SpendHeart()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound();

            var ok = HeartService.TrySpendHeart(user);
            if (!ok)
                return BadRequest(new { message = "No hearts available." });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                hearts = user.Hearts
            });
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;

            var userIdClaim = User.FindFirst("sub")
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)
                             ?? User.FindFirst("id");

            return userIdClaim != null && int.TryParse(userIdClaim.Value, out userId);
        }

        private int CalculateStreak(List<DateTime> dates)
        {
            if (dates == null || dates.Count == 0)
                return 0;

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            if (dates[0] != today && dates[0] != yesterday)
                return 0;

            int streak = 0;
            var checkDate = dates[0] == today ? today : yesterday;

            foreach (var date in dates)
            {
                if (date == checkDate)
                {
                    streak++;
                    checkDate = checkDate.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        public record AddXpRequest(int XpAmount, string Source = "lesson");
    }
}
