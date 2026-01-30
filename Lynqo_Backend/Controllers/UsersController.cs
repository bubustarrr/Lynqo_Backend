using Lynqo_Backend.Data;
using Lynqo_Backend.Models.DTOs;
// using Lynqo_Backend.Models.Services; // You can uncomment this if you have the service file
using Microsoft.AspNetCore.Authorization; // Needed for [Authorize]
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Needed for User.FindFirst

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
    // GET: api/user/me
    [HttpGet("me")]
    [Authorize] // Requires the JWT token we built earlier
    public async Task<IActionResult> GetMyProfile()
    {
        // 1. Get User ID safely (supports "sub", "NameIdentifier", or "id")
        var userIdClaim = User.FindFirst("sub")
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("id");

        if (userIdClaim == null) return Unauthorized();

        int userId = int.Parse(userIdClaim.Value);

        // 2. Fetch User Details
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // 3. Calculate Total XP
        var totalXp = await _context.UserXp // Check if your context is .UserXp or .UserXps
            .Where(x => x.UserId == userId)
            .SumAsync(x => x.XpAmount);

        // 4. Calculate Streak
        var activityDates = await _context.UserXp
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
            TotalXp = totalXp,
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

        // If last activity was before yesterday, streak is broken
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

    // --- YOUR EXISTING ENDPOINTS (Kept) ---

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
        // Direct DB access for now (simpler than Service injection for this step)
        var newXp = new Lynqo_Backend.Models.UserXp
        {
            UserId = id,
            XpAmount = request.XpAmount,
            Source = request.Source
        };
        _context.UserXp.Add(newXp);
        await _context.SaveChangesAsync();

        var totalXp = await _context.UserXp
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
