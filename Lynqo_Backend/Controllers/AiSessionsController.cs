using System.Security.Claims;
using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.DTOs;
using Lynqo_Backend.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/ai-sessions")]
[Authorize]
public class AiSessionsController : ControllerBase
{
    private readonly LynqoDbContext _context;
    private readonly AiService _ai;
    public AiSessionsController(LynqoDbContext context, AiService ai) { _context = context; _ai = ai; }

    private int GetUserId()
    {
        var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (claim == null) throw new InvalidOperationException("User ID claim missing.");
        return int.Parse(claim.Value);
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] AiStartDTO dto)
    {
        var userId = GetUserId();
        var session = new AiSession { UserId = userId, LessonId = dto.LessonId, StartTime = DateTime.UtcNow, AiScore = 0 };
        _context.AiSessions.Add(session);
        await _context.SaveChangesAsync();
        return Ok(new { sessionId = session.Id });
    }

    [HttpGet("{sessionId:int}")]
    public async Task<IActionResult> Get(int sessionId)
    {
        var userId = GetUserId();
        var session = await _context.AiSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null) return NotFound();
        return Ok(session);
    }

    [HttpPost("{sessionId:int}/messages")]
    public async Task<IActionResult> Send(int sessionId, [FromBody] AiSendMessageDTO dto)
    {
        var userId = GetUserId();
        var session = await _context.AiSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        if (session == null) return NotFound();

        _context.AiMessages.Add(new AiMessage { SessionId = sessionId, Sender = "user", Message = dto.Message, Timestamp = DateTime.UtcNow });

        var reply = await _ai.GenerateReplyAsync(session.LessonId, dto.Message);
        _context.AiMessages.Add(new AiMessage { SessionId = sessionId, Sender = "ai", Message = reply, Timestamp = DateTime.UtcNow });

        await _context.SaveChangesAsync();
        return Ok(new { reply });
    }

    [HttpPost("{sessionId:int}/complete")]
    public async Task<IActionResult> Complete(int sessionId, [FromBody] AiCompleteDTO dto)
    {
        var userId = GetUserId();
        var session = await _context.AiSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        if (session == null) return NotFound();

        session.EndTime = DateTime.UtcNow;
        session.AiScore = dto.AiScore;
        session.AiFeedback = dto.AiFeedback;

        await _context.SaveChangesAsync();
        return Ok(new { message = "AI session completed." });
    }
}
