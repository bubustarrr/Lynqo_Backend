using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using LynqoBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LynqoBackend.Models.Services
{
    public class ChatService
    {
        private readonly LynqoDbContext _context;

        public ChatService(LynqoDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanUsersChatAsync(int userId, int otherUserId)
        {
            if (userId == otherUserId) return false;

            return await _context.Friendships.AnyAsync(f =>
                f.Status == "accepted" &&
                (
                    (f.SenderId == userId && f.ReceiverId == otherUserId) ||
                    (f.SenderId == otherUserId && f.ReceiverId == userId)
                ));
        }

        public async Task<List<ConversationDto>> GetConversationsAsync(int userId)
        {
            var messages = await _context.ChatMessages
                .Where(m => !m.IsDeleted && (m.SenderId == userId || m.ReceiverId == userId))
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            var partnerIds = messages
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToList();

            var users = await _context.Users
                .Where(u => partnerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            var result = messages
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g =>
                {
                    var partnerId = g.Key;
                    var lastMessage = g.OrderByDescending(x => x.Timestamp).First();
                    var unreadCount = g.Count(x => x.ReceiverId == userId && x.ReadAt == null && !x.IsDeleted);

                    var user = users[partnerId];

                    return new ConversationDto
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName,
                        AvatarUrl = user.ProfilePicUrl,
                        LastMessage = lastMessage.Message,
                        LastMessageAt = lastMessage.Timestamp,
                        UnreadCount = unreadCount
                    };
                })
                .OrderByDescending(x => x.LastMessageAt)
                .ToList();

            return result;
        }

        public async Task<List<ChatMessageDto>> GetMessagesAsync(int userId, int otherUserId)
        {
            var canChat = await CanUsersChatAsync(userId, otherUserId);
            if (!canChat) throw new Exception("You can only chat with accepted friends.");

            return await _context.ChatMessages
                .Where(m => !m.IsDeleted &&
                    (
                        (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                        (m.SenderId == otherUserId && m.ReceiverId == userId)
                    ))
                .OrderBy(m => m.Timestamp)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Message = m.Message,
                    Timestamp = m.Timestamp,
                    ReadAt = m.ReadAt,
                    IsMine = m.SenderId == userId
                })
                .ToListAsync();
        }

        public async Task<ChatMessageDto> SendMessageAsync(int senderId, SendMessageDto dto)
        {
            if (dto.ReceiverId == senderId)
                throw new Exception("You cannot message yourself.");

            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new Exception("Message cannot be empty.");

            var canChat = await CanUsersChatAsync(senderId, dto.ReceiverId);
            if (!canChat) throw new Exception("You can only chat with accepted friends.");

            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                Message = dto.Message.Trim(),
                Timestamp = DateTime.UtcNow,
                ReadAt = null,
                IsDeleted = false,
                IsReported = false
            };

            _context.ChatMessages.Add(message);

            var receiverSettings = await _context.Settings
                .FirstOrDefaultAsync(s => s.UserId == dto.ReceiverId);

            if (receiverSettings == null || receiverSettings.NotificationsEnabled)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = dto.ReceiverId,
                    Type = "chat_message",
                    Message = "You received a new message.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return new ChatMessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Message = message.Message,
                Timestamp = message.Timestamp,
                ReadAt = message.ReadAt,
                IsMine = true
            };
        }

        public async Task<int> MarkConversationAsReadAsync(int userId, int otherUserId)
        {
            var unreadMessages = await _context.ChatMessages
                .Where(m =>
                    m.SenderId == otherUserId &&
                    m.ReceiverId == userId &&
                    m.ReadAt == null &&
                    !m.IsDeleted)
                .ToListAsync();

            foreach (var msg in unreadMessages)
            {
                msg.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return unreadMessages.Count;
        }
    }
}
