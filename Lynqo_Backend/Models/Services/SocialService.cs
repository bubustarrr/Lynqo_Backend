using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
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
                    FriendshipId = f.Id,
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
            // ONLY get requests where WE are the receiver (people waiting for us to respond)
            var requests = await _context.Friendships
                .Where(f => f.Status == "pending" && f.ReceiverId == userId)
                .Include(f => f.Sender)
                .ToListAsync();

            return requests.Select(f => new FriendDTO
            {
                FriendshipId = f.Id,
                UserId = f.SenderId,
                Username = f.Sender.Username,
                DisplayName = f.Sender.DisplayName,
                Status = f.Status,
                IsSender = false
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
            // Find the exact Friendship DB row
            var friendship = await _context.Friendships.FindAsync(requestId)
                             ?? throw new InvalidOperationException("Request not found.");

            // Ensure I am the one receiving this request
            if (friendship.ReceiverId != userId)
                throw new InvalidOperationException("Not allowed to respond to this request.");

            if (accept)
            {
                friendship.Status = "accepted";
            }
            else
            {
                // If declined, usually we just delete the request entirely so they can try again later
                _context.Friendships.Remove(friendship);
            }

            await _context.SaveChangesAsync();
        }
        // Add this inside SocialService.cs
        public async Task RemoveFriendAsync(int requestingUserId, int friendUserId)
        {
            // Megkeressük a barátságot, ahol ez a két felhasználó szerepel
            var friendship = await _context.Friendships.FirstOrDefaultAsync(f =>
                (f.SenderId == requestingUserId && f.ReceiverId == friendUserId) ||
                (f.SenderId == friendUserId && f.ReceiverId == requestingUserId));

            if (friendship == null)
            {
                throw new Exception("Friendship not found.");
            }

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
        }
    }

}
