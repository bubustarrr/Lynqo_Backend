using Lynqo_Backend.Data;
using Lynqo_Backend.Models.DTOs;
using Lynqo_Backend.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly LynqoDbContext _context;
    public UserController(LynqoDbContext context) { _context = context; }

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
        var service = new GamificationService(_context); // Temp until DI
        await service.AddXpAsync(id, request.XpAmount, request.Source);

        var totalXp = await _context.UserXps
            .Where(xp => xp.UserId == id)
            .SumAsync(xp => xp.XpAmount);

        return Ok(new { totalXp, message = "XP added!" });
    }

    [HttpGet("{id}/xp")]
    public async Task<IActionResult> GetUserXp(int id)
    {
        var totalXp = await _context.UserXps
            .Where(xp => xp.UserId == id)
            .SumAsync(xp => xp.XpAmount);

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

        return Ok(new UserXpDTO { TotalXp = totalXp, RecentXp = recent });
    }
    public record AddXpRequest(int XpAmount, string Source = "lesson");
}
