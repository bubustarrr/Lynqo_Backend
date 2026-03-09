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
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chat;

        public ChatController(ChatService chat)
        {
            _chat = chat;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("id")
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("sub");

            if (claim == null) throw new InvalidOperationException("User ID missing.");
            return int.Parse(claim.Value);
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = GetUserId();
            var result = await _chat.GetConversationsAsync(userId);
            return Ok(result);
        }

        [HttpGet("{otherUserId}")]
        public async Task<IActionResult> GetMessages(int otherUserId)
        {
            var userId = GetUserId();
            var result = await _chat.GetMessagesAsync(userId, otherUserId);
            return Ok(result);
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            var userId = GetUserId();
            var result = await _chat.SendMessageAsync(userId, dto);
            return Ok(result);
        }

        [HttpPost("{otherUserId}/read")]
        public async Task<IActionResult> MarkAsRead(int otherUserId)
        {
            var userId = GetUserId();
            var count = await _chat.MarkConversationAsReadAsync(userId, otherUserId);
            return Ok(new { updated = count });
        }
    }
}
