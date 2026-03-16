using Lynqo_Backend.Data;
using Lynqo_Backend.Models.DTOs;
using Lynqo_Backend.Services;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> GetUserBadges(int userId, [FromServices] BadgeService badgeService)
    {
        // 1. Evaluate & award missing badges dynamically
        await badgeService.EvaluateBadgesAsync(userId);

        // 2. Fetch ALL badges
        var allBadges = await _context.Badges.ToListAsync();

        // 3. Fetch IDs of badges the user owns
        var userBadgeIds = await _context.UserBadges
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.BadgeId)
            .ToListAsync();

        // 4. Return combined result
        var result = allBadges.Select(b => new
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            IconUrl = b.IconUrl,
            IsOwned = userBadgeIds.Contains(b.Id)
        });

        return Ok(result);
    }

}
