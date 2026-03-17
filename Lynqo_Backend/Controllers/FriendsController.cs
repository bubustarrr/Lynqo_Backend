using System.Security.Claims;
using LynqoBackend.Models.DTOs;
using LynqoBackend.Models.Services;
using Lynqo_Backend.Data; // Az adatbázisodhoz szükséges using
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LynqoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendsController : ControllerBase
    {
        private readonly SocialService _social;
        private readonly LynqoDbContext _context;
        public FriendsController(SocialService social, LynqoDbContext context)
        {
            _social = social;
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
        public async Task<IActionResult> GetFriends()
        {
            var userId = GetUserId();
            var friends = await _social.GetFriendsAsync(userId);
            return Ok(friends);
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetRequests()
        {
            var userId = GetUserId();
            var requests = await _social.GetRequestsAsync(userId);
            return Ok(requests);
        }

        [HttpPost("request")]
        public async Task<IActionResult> SendRequest([FromBody] FriendRequestDTO dto)
        {
            var userId = GetUserId();
            await _social.SendRequestAsync(userId, dto.TargetUserId);
            return Ok(new { message = "Friend request sent." });
        }

        [HttpPost("request-by-identifier")]
        public async Task<IActionResult> SendRequestByIdentifier([FromBody] FriendRequestByIdentifierDTO dto)
        {
            var userId = GetUserId();

            // Keresés az adatbázisban a Név vagy Email alapján
            var targetUser = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == dto.Identifier ||
                u.Email == dto.Identifier);

            if (targetUser == null)
                return NotFound(new { message = "Nem található felhasználó ezzel a névvel vagy email címmel." });

            if (targetUser.Id == userId)
                return BadRequest(new { message = "Magadat nem veheted fel barátnak!" });

            // Mentés a SocialService-en keresztül
            try
            {
                await _social.SendRequestAsync(userId, targetUser.Id);
                return Ok(new { message = "Barátnak jelölés sikeresen elküldve!" });
            }
            catch (Exception ex)
            {
                // Ha már barátok, vagy van függõben lévõ kérés
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("respond")]
        public async Task<IActionResult> Respond([FromBody] FriendRespondDTO dto)
        {
            var userId = GetUserId();
            await _social.RespondRequestAsync(userId, dto.RequestId, dto.Accept);
            return Ok(new { message = dto.Accept ? "Friend request accepted." : "Friend request declined." });
        }

        [HttpDelete("{friendUserId}")]
        public async Task<IActionResult> Unfriend(int friendUserId)
        {
            var userId = GetUserId();

            try
            {
                await _social.RemoveFriendAsync(userId, friendUserId);
                return Ok(new { message = "Friend removed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}