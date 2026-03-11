using Lynqo_Backend.Data;
using Lynqo_Backend.Models.DTOs;
using LynqoBackend.Models;
using LynqoBackend.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            var xp = new UserXp { UserId = userId, XpAmount = xpAmount, Source = source, CreatedAt = DateTime.UtcNow };
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
            var quests = await _context.Quests.ToListAsync();
            var today = DateTime.UtcNow.Date;

            var userQuests = await _context.UserQuests
                .Where(uq => uq.UserId == userId)
                .ToListAsync();

            var result = new List<QuestDTO>();

            foreach (var q in quests)
            {
                var uq = userQuests.FirstOrDefault(u => u.QuestId == q.Id);

                int currentProgress = 0;
                bool isCompleted = false;

                if (uq != null)
                {
                    // Check if the progress was made today
                    bool completedToday = uq.CompletedAt.HasValue && uq.CompletedAt.Value.Date == today;

                    // DAILY REFRESH: If it was completed on a previous day, send 0 progress to React!
                    if (uq.CompletedAt.HasValue && uq.CompletedAt.Value.Date < today)
                    {
                        currentProgress = 0;
                        isCompleted = false;
                    }
                    else
                    {
                        currentProgress = uq.Progress;
                        isCompleted = completedToday;
                    }
                }

                int targetAmount = q.TargetAmount > 0 ? q.TargetAmount : 1;

                result.Add(new QuestDTO
                {
                    Id = q.Id,
                    Title = q.Title,
                    Description = q.Description,
                    RewardXp = q.RewardXp,
                    Duration = q.Duration,
                    Type = q.Type,
                    Target = targetAmount,
                    Progress = currentProgress,
                    IsCompleted = isCompleted
                });
            }

            return result;
        }

        public async Task UpdateQuestProgressAsync(int userId, int questId, int delta)
        {
            var quest = await _context.Quests.FindAsync(questId);
            if (quest == null) return;

            var userQuest = await _context.UserQuests
                .FirstOrDefaultAsync(uq => uq.UserId == userId && uq.QuestId == questId);

            var today = DateTime.UtcNow.Date;
            int targetAmount = quest.TargetAmount > 0 ? quest.TargetAmount : 1;

            if (userQuest == null)
            {
                // First time ever doing this quest
                userQuest = new UserQuest
                {
                    UserId = userId,
                    QuestId = questId,
                    Progress = delta,
                    CompletedAt = null // we use CompletedAt as a timestamp
                };
                _context.UserQuests.Add(userQuest);
            }
            else
            {
                // DAILY REFRESH: If the user quest was updated on a PREVIOUS day, reset it for today
                if (userQuest.CompletedAt.HasValue && userQuest.CompletedAt.Value.Date < today)
                {
                    userQuest.Progress = 0;
                    userQuest.CompletedAt = null;
                }

                // Only add progress if it's not already completed TODAY
                if (!userQuest.CompletedAt.HasValue || userQuest.CompletedAt.Value.Date < today)
                {
                    userQuest.Progress += delta;
                }
            }

            // --- AUTO-CLAIM LOGIC ---
            // If they just hit the target...
            if (userQuest.Progress >= targetAmount && (!userQuest.CompletedAt.HasValue || userQuest.CompletedAt.Value.Date < today))
            {
                // Lock it at max target
                userQuest.Progress = targetAmount;

                // Mark it as completed today
                userQuest.CompletedAt = DateTime.UtcNow;

                // Automatically add the XP to the user's account right now!
                var xpEntry = new UserXp
                {
                    UserId = userId,
                    XpAmount = quest.RewardXp,
                    Source = "legendary", // Tag it so you know it came from a quest
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserXps.Add(xpEntry);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> ClaimQuestRewardAsync(int userId, int questId)
        {
            return await Task.FromResult(0);
        }
    }
}
