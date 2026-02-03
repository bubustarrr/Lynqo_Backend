using System.Security.Claims;
using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly LynqoDbContext _context;
    public AnalyticsController(LynqoDbContext context) => _context = context;

    private int GetUserId()
    {
        var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (claim == null) throw new InvalidOperationException("User ID claim missing.");
        return int.Parse(claim.Value);
    }

    [HttpPost("lesson")]
    public async Task<IActionResult> AddLessonAnalytics([FromBody] LessonAnalyticsCreateDTO dto)
    {
        var userId = GetUserId();
        _context.Analytics.Add(new Analytics
        {
            UserId = userId,
            LessonId = dto.LessonId,
            TimeSpentSeconds = dto.TimeSpentSeconds,
            Accuracy = dto.Accuracy,
            Attempts = dto.Attempts,
            CompletedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return Ok(new { message = "Analytics saved." });
    }

    [HttpPost("practice")]
    public async Task<IActionResult> AddPractice([FromBody] PracticeSessionCreateDTO dto)
    {
        var userId = GetUserId();
        _context.PracticeSessions.Add(new PracticeSession
        {
            UserId = userId,
            Type = dto.Type,
            Score = dto.Score,
            XpEarned = dto.XpEarned,
            DurationSeconds = dto.DurationSeconds,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return Ok(new { message = "Practice saved." });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetUserId();

        var lessonCount = await _context.Analytics.CountAsync(a => a.UserId == userId);
        var totalTime = await _context.Analytics.Where(a => a.UserId == userId).SumAsync(a => a.TimeSpentSeconds);
        var avgAccuracy = await _context.Analytics.Where(a => a.UserId == userId).AverageAsync(a => (double)a.Accuracy);

        var practiceCount = await _context.PracticeSessions.CountAsync(p => p.UserId == userId);

        return Ok(new { lessonCount, totalTimeSeconds = totalTime, avgAccuracy, practiceCount });
    }
}
