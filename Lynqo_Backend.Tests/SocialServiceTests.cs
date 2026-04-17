using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Lynqo_Backend.Data;
using LynqoBackend.Models.Services;
using Lynqo_Backend.Models;

namespace Lynqo_Backend.Tests
{
    public class SocialServiceTests
    {
        [Fact]
        public async Task SendRequestAsync_WhenSenderAndTargetAreSame_ShouldThrowException()
        {
            var options = new DbContextOptionsBuilder<LynqoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LynqoDbContext(options);
            var socialService = new SocialService(context);

            int userId = 5;

            Func<Task> act = async () => await socialService.SendRequestAsync(userId, userId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Cannot add yourself.");
        }

        [Fact]
        public async Task SendRequestAsync_WhenValid_ShouldCreatePendingFriendship()
        {
            var options = new DbContextOptionsBuilder<LynqoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LynqoDbContext(options);
            var socialService = new SocialService(context);

            int senderId = 1;
            int targetId = 2;

            await socialService.SendRequestAsync(senderId, targetId);

            var request = await context.Friendships.FirstOrDefaultAsync();
            request.Should().NotBeNull();
            request!.SenderId.Should().Be(senderId);
            request.ReceiverId.Should().Be(targetId);
            request.Status.Should().Be("pending");
        }

        [Fact]
        public async Task RespondRequestAsync_WhenAccepted_ShouldChangeStatusToAccepted()
        {
            var options = new DbContextOptionsBuilder<LynqoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LynqoDbContext(options);

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

            await socialService.RespondRequestAsync(userId: 2, requestId: 10, accept: true);

            var updatedRequest = await context.Friendships.FindAsync(10);
            updatedRequest.Should().NotBeNull();
            updatedRequest!.Status.Should().Be("accepted");
        }
    }
}