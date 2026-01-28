using Microsoft.AspNetCore.Mvc;
using Lynqo_Backend.Data;
using Lynqo_Backend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class BadgesController : ControllerBase
{
    private readonly LynqoDbContext _context;

    public BadgesController(LynqoDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var badges = await _context.Badges.ToListAsync();
        return Ok(badges.Select(b => new BadgeDTO
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            IconUrl = b.IconUrl
        }));
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserBadges(int userId)
    {
        var badges = await _context.UserBadges
            .Where(ub => ub.UserId == userId)
            .Include(ub => ub.Badge)
            .Select(ub => new BadgeDTO
            {
                Id = ub.Badge.Id,
                Name = ub.Badge.Name,
                Description = ub.Badge.Description,
                IconUrl = ub.Badge.IconUrl
            })
            .ToListAsync();
        return Ok(badges);
    }
}
