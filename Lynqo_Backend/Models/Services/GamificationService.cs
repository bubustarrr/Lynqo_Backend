using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.DTOs;
using LynqoBackend.Models;
using LynqoBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Lynqo_Backend.Models.Services
{
    public class GamificationService
    {
        private readonly LynqoDbContext _context;

        public GamificationService(LynqoDbContext context)
        {
            _context = context;
        }

        public async Task AddXpAsync(int userId, int xpAmount, string source = "lesson")
        {
            var xp = new UserXp { UserId = userId, XpAmount = xpAmount, Source = source };
            _context.UserXps.Add(xp);
            await _context.SaveChangesAsync();
        }

        public async Task<List<LeaderboardEntryDTO>> GetLeaderboardAsync(int leaderboardId)
        {
            return await _context.LeaderboardEntries
                .Where(le => le.LeaderboardId == leaderboardId)
                .Include(le => le.User)
                .OrderByDescending(le => le.Xp)
                .ThenBy(le => le.Rank)
                .Take(100)
                .Select(le => new LeaderboardEntryDTO
                {
                    UserId = le.UserId,
                    Username = le.User.Username,
                    DisplayName = le.User.DisplayName,
                    Xp = le.Xp,
                    Rank = le.Rank
                })
                .ToListAsync();
        }

        public async Task<List<QuestDTO>> GetActiveQuestsAsync(int userId)
        {
            // For now: all quests are “active”; later you can filter by date/duration.
            var quests = await _context.Quests.ToListAsync();

            var userQuestLookup = await _context.UserQuests
                .Where(uq => uq.UserId == userId)
                .ToDictionaryAsync(uq => uq.QuestId, uq => uq);

            return quests.Select(q =>
            {
                userQuestLookup.TryGetValue(q.Id, out var uq);
                return new QuestDTO
                {
                    Id = q.Id,
                    Title = q.Title,
                    Description = q.Description,
                    RewardXp = q.RewardXp,
                    Duration = q.Duration,
                    Type = q.Type,
                    Progress = uq?.Progress ?? 0,
                    IsCompleted = uq?.CompletedAt != null
                };
            }).ToList();
        }

        public async Task UpdateQuestProgressAsync(int userId, int questId, int delta)
        {
            var quest = await _context.Quests.FindAsync(questId);
            if (quest == null) throw new InvalidOperationException("Quest not found.");

            var userQuest = await _context.UserQuests
                .FirstOrDefaultAsync(uq => uq.UserId == userId && uq.QuestId == questId);

            if (userQuest == null)
            {
                userQuest = new UserQuest
                {
                    UserId = userId,
                    QuestId = questId,
                    Progress = Math.Max(0, delta),
                    CompletedAt = null
                };
                _context.UserQuests.Add(userQuest);
            }
            else
            {
                userQuest.Progress = Math.Max(0, userQuest.Progress + delta);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> ClaimQuestRewardAsync(int userId, int questId)
        {
            var quest = await _context.Quests.FindAsync(questId);
            if (quest == null) throw new InvalidOperationException("Quest not found.");

            var userQuest = await _context.UserQuests
                .FirstOrDefaultAsync(uq => uq.UserId == userId && uq.QuestId == questId);

            if (userQuest == null || userQuest.CompletedAt != null)
                throw new InvalidOperationException("Quest not completed or already claimed.");

            // Mark as completed now (you can also enforce a min progress if you add that field)
            userQuest.CompletedAt = DateTime.UtcNow;

            await AddXpAsync(userId, quest.RewardXp, "lesson"); // or "practice" based on quest.Type

            await _context.SaveChangesAsync();

            return quest.RewardXp;
        }
    }
}
