using System.Security.Claims;
using Lynqo_Backend.Data;
using LynqoBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LynqoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly LynqoDbContext _context;

        public NotificationsController(LynqoDbContext context)
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

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = GetUserId();

            var list = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDTO
                {
                    Id = n.Id,
                    Type = n.Type,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = GetUserId();

            var n = await _context.Notifications
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (n == null) return NotFound();

            n.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Marked as read." });
        }
    }
}
