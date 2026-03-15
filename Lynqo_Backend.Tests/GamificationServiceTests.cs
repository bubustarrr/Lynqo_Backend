using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Lynqo_Backend.Data;
using Lynqo_Backend.Models.Services;
using LynqoBackend.Models; // Vagy Lynqo_Backend.Models

namespace Lynqo_Backend.Tests
{
    public class GamificationServiceTests
    {
        [Fact]
        public async Task AddXpAsync_ShouldSaveNewXpEntryToDatabase()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<LynqoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LynqoDbContext(options);
            var gamificationService = new GamificationService(context);

            int userId = 1;
            int xpAmount = 50;

            // Act
            await gamificationService.AddXpAsync(userId, xpAmount, "lesson");

            // Assert
            var savedXp = await context.UserXps.FirstOrDefaultAsync();

            savedXp.Should().NotBeNull();
            savedXp!.UserId.Should().Be(userId);
            savedXp.XpAmount.Should().Be(xpAmount);
            savedXp.Source.Should().Be("lesson");
        }

        [Fact]
        public async Task UpdateQuestProgressAsync_ShouldIncreaseUserProgress()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<LynqoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LynqoDbContext(options);

            // Küldetés a memóriában
            context.Quests.Add(new Quest { Id = 1, TargetAmount = 50, RewardXp = 100 });
            await context.SaveChangesAsync();

            var gamificationService = new GamificationService(context);

            // Act: Szerez 10 pontot
            await gamificationService.UpdateQuestProgressAsync(userId: 1, questId: 1, delta: 10);

            // Assert
            var userQuest = await context.UserQuests.FirstOrDefaultAsync(uq => uq.UserId == 1 && uq.QuestId == 1);

            userQuest.Should().NotBeNull();
            userQuest!.Progress.Should().Be(10);
            userQuest.CompletedAt.Should().BeNull();
        }
    }
}