using Microsoft.AspNetCore.Mvc;
using Lynqo_Backend.Data;
using Lynqo_Backend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Lynqo_Backend.Models.Services;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardsController : ControllerBase
{
    private readonly LynqoDbContext _context;
    private readonly GamificationService _gamification;

    public LeaderboardsController(LynqoDbContext context, GamificationService gamification)
    {
        _context = context;
        _gamification = gamification;
    }

    [HttpGet]
    public async Task<IActionResult> GetLeaderboards()
    {
        var boards = await _context.Leaderboards.ToListAsync();
        return Ok(boards);
    }

    [HttpGet("{id}/entries")]
    public async Task<IActionResult> GetEntries(int id)
    {
        var entries = await _gamification.GetLeaderboardAsync(id);
        return Ok(entries);
    }
}
