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
    public class StoreController : ControllerBase
    {
        private readonly StoreService _storeService;

        public StoreController(StoreService storeService)
        {
            _storeService = storeService;
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

        [HttpGet("items")]
        [AllowAnonymous] // store list is public if you want
        public async Task<IActionResult> GetItems()
        {
            var items = await _storeService.GetItemsAsync();
            return Ok(items);
        }

        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory()
        {
            var userId = GetUserId();
            var items = await _storeService.GetInventoryAsync(userId);
            return Ok(items);
        }

        [HttpPost("purchase")]
        public async Task<IActionResult> Purchase([FromBody] PurchaseRequest dto)
        {
            var userId = GetUserId();
            await _storeService.PurchaseAsync(userId, dto.ItemId, dto.Quantity);
            return Ok(new { message = "Purchase successful." });
        }
    }
}
