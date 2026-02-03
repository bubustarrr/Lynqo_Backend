using System.Security.Claims;
using LynqoBackend.Models.DTOs;
using LynqoBackend.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LynqoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendsController : ControllerBase
    {
        private readonly SocialService _social;

        public FriendsController(SocialService social)
        {
            _social = social;
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

        [HttpPost("respond")]
        public async Task<IActionResult> Respond([FromBody] FriendRespondDTO dto)
        {
            var userId = GetUserId();
            await _social.RespondRequestAsync(userId, dto.RequestId, dto.Accept);
            return Ok(new { message = dto.Accept ? "Friend request accepted." : "Friend request declined." });
        }
    }
}
