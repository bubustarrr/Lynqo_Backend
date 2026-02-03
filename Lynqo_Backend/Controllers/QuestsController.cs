using System.Security.Claims;
using Lynqo_Backend.Models.Services;
using Lynqo_Backend.Data;
using LynqoBackend.Models.DTOs;
using LynqoBackend.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LynqoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // all endpoints require JWT
    public class QuestsController : ControllerBase
    {
        private readonly GamificationService _gamificationService;

        public QuestsController(GamificationService gamificationService)
        {
            _gamificationService = gamificationService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("id")
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("sub");

            if (claim == null)
                throw new InvalidOperationException("User ID claim missing.");

            return int.Parse(claim.Value);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var userId = GetUserId();
            var quests = await _gamificationService.GetActiveQuestsAsync(userId);
            return Ok(quests);
        }

        [HttpPost("progress")]
        public async Task<IActionResult> UpdateProgress([FromBody] QuestProgressRequest dto)
        {
            var userId = GetUserId();
            await _gamificationService.UpdateQuestProgressAsync(userId, dto.QuestId, dto.ProgressDelta);
            return Ok(new { message = "Progress updated." });
        }

        [HttpPost("claim")]
        public async Task<IActionResult> Claim([FromBody] QuestClaimRequest dto)
        {
            var userId = GetUserId();
            var xpAwarded = await _gamificationService.ClaimQuestRewardAsync(userId, dto.QuestId);
            return Ok(new { message = "Quest reward claimed.", xpAwarded });
        }
    }
}
