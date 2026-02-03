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
    public class SubscriptionsController : ControllerBase
    {
        private readonly SubscriptionService _subscriptionService;

        public SubscriptionsController(SubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
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

        [HttpGet("me")]
        public async Task<IActionResult> GetMySubscription()
        {
            var userId = GetUserId();
            var sub = await _subscriptionService.GetCurrentAsync(userId);
            return Ok(sub);
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] StartSubscriptionRequest dto)
        {
            var userId = GetUserId();
            var sub = await _subscriptionService.StartAsync(userId, dto.PlanName, dto.QuantityMonths);
            return Ok(sub);
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel()
        {
            var userId = GetUserId();
            await _subscriptionService.CancelAsync(userId);
            return Ok(new { message = "Subscription cancelled (auto‑renew disabled)." });
        }
    }
}
