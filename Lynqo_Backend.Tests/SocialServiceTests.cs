using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Lynqo_Backend.Data;
using LynqoBackend.Models.Services;
using Lynqo_Backend.Models; // Vagy LynqoBackend.Models, attól függően, hol van a Friendship.cs

namespace Lynqo_Backend.Tests
{
    public class SocialServiceTests
    {
        [Fact]
        public async Task SendRequestAsync_WhenSenderAndTargetAreSame_ShouldThrowException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<LynqoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LynqoDbContext(options);
            var socialService = new SocialService(context);

            int userId = 5;

            // Act
            Func<Task> act = async () => await socialService.SendRequestAsync(userId, userId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Cannot add yourself.");
        }

        [Fact]
        public async Task SendRequestAsync_WhenValid_ShouldCreatePendingFriendship()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<LynqoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LynqoDbContext(options);
            var socialService = new SocialService(context);

            int senderId = 1;
            int targetId = 2;

            // Act
            await socialService.SendRequestAsync(senderId, targetId);

            // Assert
            var request = await context.Friendships.FirstOrDefaultAsync();
            request.Should().NotBeNull();
            request!.SenderId.Should().Be(senderId);
            request.ReceiverId.Should().Be(targetId);
            request.Status.Should().Be("pending");
        }

        [Fact]
        public async Task RespondRequestAsync_WhenAccepted_ShouldChangeStatusToAccepted()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<LynqoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LynqoDbContext(options);

            // Létrehozunk egy pending kérelmet
            var pendingRequest = new Friendship
            {
                Id = 10,
                SenderId = 1,
                ReceiverId = 2,
                Status = "pending"
            };
            context.Friendships.Add(pendingRequest);
            await context.SaveChangesAsync();

            var socialService = new SocialService(context);

            // Act: A 2-es user elfogadja a 10-es azonosítójú kérelmet
            await socialService.RespondRequestAsync(userId: 2, requestId: 10, accept: true);

            // Assert
            var updatedRequest = await context.Friendships.FindAsync(10);
            updatedRequest.Should().NotBeNull();
            updatedRequest!.Status.Should().Be("accepted");
        }
    }
}