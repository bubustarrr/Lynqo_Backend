using Lynqo_Backend.Data;
using Lynqo_Backend.Models; // Ensure this matches your UserXp model namespace
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

        // --- NEW: The "Me" Endpoint for the Home Screen ---
        // GET: api/user/me?courseId=1
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile([FromQuery] int? courseId)
        {
            // 1. Get User ID safely
            var userIdClaim = User.FindFirst("sub")
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("id");

            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            // 2. Fetch User Details
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // 3. Calculate XP (Separating Course XP vs Global XP)

            // A. Global Lifetime XP (Sum of all history)
            // 🔥 This replaces the missing 'TotalXp' column on the User table
            var globalXp = await _context.UserXp // If red, change to UserXps
                .Where(x => x.UserId == userId)
                .SumAsync(x => x.XpAmount);

            // B. Active Course XP (from UserCourses table)
            int currentCourseXp = 0;
            if (courseId.HasValue)
            {
                // If a course is selected, get XP for THAT course specifically
                var userCourse = await _context.UserCourses
                    .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CourseId == courseId.Value);

                if (userCourse != null)
                {
                    currentCourseXp = userCourse.TotalXp;
                }
            }
            else
            {
                // Fallback: If no course specified, show global XP
                currentCourseXp = globalXp;
            }

            // 4. Calculate Streak (Global)
            var activityDates = await _context.UserXp // If red, change to UserXps
                .Where(x => x.UserId == userId)
                .Select(x => x.CreatedAt.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();

            int currentStreak = CalculateStreak(activityDates);

            // 5. Return the combined data
            return Ok(new
            {
                user.Id,
                user.Username,
                user.DisplayName,
                user.ProfilePicUrl,
                user.Hearts,
                user.Coins,
                user.IsPremium,

                // The frontend expects "TotalXp" for the dashboard card.
                // We send either the Course XP (if courseId sent) or Global XP.
                TotalXp = currentCourseXp > 0 ? currentCourseXp : globalXp,

                // We also send Global XP explicitly if needed for profile
                LifetimeXp = globalXp,

                Streak = currentStreak
            });
        }

        // --- Helper for Streak Calculation ---
        private int CalculateStreak(List<DateTime> dates)
        {
            if (dates.Count == 0) return 0;

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);
            int streak = 0;

            if (dates[0] != today && dates[0] != yesterday)
            {
                return 0;
            }

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

        // --- EXISTING ENDPOINTS ---

        [HttpGet("{username}")]
        public async Task<IActionResult> GetUser(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound();
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
            var newXp = new Lynqo_Backend.Models.UserXp
            {
                UserId = id,
                XpAmount = request.XpAmount,
                Source = request.Source
            };
            _context.UserXp.Add(newXp); // If red, change to UserXps
            await _context.SaveChangesAsync();

            var totalXp = await _context.UserXp // If red, change to UserXps
                .Where(xp => xp.UserId == id)
                .SumAsync(xp => xp.XpAmount);

            return Ok(new { totalXp, message = "XP added!" });
        }

        [HttpGet("{id}/xp")]
        public async Task<IActionResult> GetUserXp(int id)
        {
            var totalXp = await _context.UserXp
                .Where(xp => xp.UserId == id)
                .SumAsync(xp => xp.XpAmount);

            var recent = await _context.UserXp
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

            return Ok(new UserXpDTO { TotalXp = totalXp, RecentXp = recent });
        }

        public record AddXpRequest(int XpAmount, string Source = "lesson");
    }
}
