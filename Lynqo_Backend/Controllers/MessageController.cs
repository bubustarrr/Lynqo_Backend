using System.Security.Claims;
using Lynqo_Backend.Data;
using LynqoBackend.Models;
using LynqoBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LynqoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly LynqoDbContext _context;

        public MessagesController(LynqoDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("id")
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("sub");
            if (claim == null) throw new InvalidOperationException("User ID missing.");
            return int.Parse(claim.Value);
        }

        [HttpGet("thread/{otherUserId:int}")]
        public async Task<IActionResult> GetThread(int otherUserId)
        {
            var userId = GetUserId();

            var messages = await _context.ChatMessages
                .Where(m =>
                    (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                    (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new ChatMessageDTO
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Message = m.Message,
                    Timestamp = m.Timestamp,
                    IsDeleted = m.IsDeleted
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendMessageDTO dto)
        {
            var userId = GetUserId();

            var msg = new ChatMessage
            {
                SenderId = userId,
                ReceiverId = dto.ReceiverId,
                Message = dto.Message,
                Timestamp = DateTime.UtcNow,
                IsDeleted = false,
                IsReported = false
            };

            _context.ChatMessages.Add(msg);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sent.", id = msg.Id });
        }

        [HttpPost("{id:int}/report")]
        public async Task<IActionResult> Report(int id, [FromBody] ReportMessageDTO dto)
        {
            var userId = GetUserId();

            var msg = await _context.ChatMessages.FindAsync(id);
            if (msg == null) return NotFound("Message not found.");

            msg.IsReported = true;

            var report = new Report
            {
                ReporterId = userId,
                MessageId = msg.Id,
                Reason = dto.Reason,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Reported." });
        }
    }
}
