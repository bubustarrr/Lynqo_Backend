using Lynqo_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LynqoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PowerupsController : ControllerBase
    {
        private readonly LynqoDbContext _context;

        private const int HeartRefillCost = 350;
        private const int MaxHearts = 5;

        public PowerupsController(LynqoDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("id")
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("sub");

            if (claim == null) throw new InvalidOperationException("User ID claim missing.");
            return int.Parse(claim.Value);
        }

        // POST api/powerups/refill_hearts
        [HttpPost("refill_hearts")]
        public async Task<IActionResult> RefillHearts()
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });

            if (user.Hearts >= MaxHearts)
                return BadRequest(new { message = "Your hearts are already full!" });

            if (user.Coins < HeartRefillCost)
                return BadRequest(new { message = $"Not enough gems. You need {HeartRefillCost} 💎." });

            user.Coins -= HeartRefillCost;
            user.Hearts = MaxHearts;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Hearts refilled!",
                hearts = user.Hearts,
                coins = user.Coins
            });
        }
    }
}
