using Lynqo_Backend.Data;
using LynqoBackend.Models;
using LynqoBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;


namespace LynqoBackend.Models.Services
{
    public class SocialService
    {
        private readonly LynqoDbContext _context;

        public SocialService(LynqoDbContext context)
        {
            _context = context;
        }

        public async Task<List<FriendDTO>> GetFriendsAsync(int userId)
        {
            var friendships = await _context.Friendships
                .Where(f => f.Status == "accepted" &&
                           (f.SenderId == userId || f.ReceiverId == userId))
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .ToListAsync();

            return friendships.Select(f =>
            {
                var other = f.SenderId == userId ? f.Receiver : f.Sender;
                return new FriendDTO
                {
                    UserId = other.Id,
                    Username = other.Username,
                    DisplayName = other.DisplayName,
                    Status = f.Status,
                    IsSender = f.SenderId == userId
                };
            }).ToList();
        }

        public async Task<List<FriendDTO>> GetRequestsAsync(int userId)
        {
            var requests = await _context.Friendships
                .Where(f => f.Status == "pending" &&
                           (f.SenderId == userId || f.ReceiverId == userId))
                .Include(f => f.Sender)
                .Include(f => f.Receiver)
                .ToListAsync();

            return requests.Select(f =>
            {
                var other = f.SenderId == userId ? f.Receiver : f.Sender;
                return new FriendDTO
                {
                    UserId = other.Id,
                    Username = other.Username,
                    DisplayName = other.DisplayName,
                    Status = f.Status,
                    IsSender = f.SenderId == userId
                };
            }).ToList();
        }

        public async Task SendRequestAsync(int senderId, int targetUserId)
        {
            if (senderId == targetUserId)
                throw new InvalidOperationException("Cannot add yourself.");

            var exists = await _context.Friendships.AnyAsync(f =>
                (f.SenderId == senderId && f.ReceiverId == targetUserId) ||
                (f.SenderId == targetUserId && f.ReceiverId == senderId));

            if (exists)
                throw new InvalidOperationException("Friendship already exists or pending.");

            var friendship = new Friendship
            {
                SenderId = senderId,
                ReceiverId = targetUserId,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();
        }

        public async Task RespondRequestAsync(int userId, int requestId, bool accept)
        {
            var friendship = await _context.Friendships.FindAsync(requestId)
                             ?? throw new InvalidOperationException("Request not found.");

            if (friendship.ReceiverId != userId)
                throw new InvalidOperationException("Not allowed to respond to this request.");

            friendship.Status = accept ? "accepted" : "declined";
            await _context.SaveChangesAsync();
        }
    }
}
