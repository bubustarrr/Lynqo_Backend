using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Lynqo_Backend.Data;
using Lynqo_Backend.Controllers;
using Lynqo_Backend.Models;

namespace Lynqo_Backend.Tests
{
    public class UserControllerTests
    {
        [Fact]
        public void CalculateStreak_WhenConsecutiveDays_ShouldReturnCorrectStreak()
        {
            var controller = new UserController(null!);
            var methodInfo = typeof(UserController).GetMethod("CalculateStreak", BindingFlags.NonPublic | BindingFlags.Instance);
            var today = DateTime.UtcNow.Date;

            var activityDates = new List<DateTime>
            {
                today,
                today.AddDays(-1),
                today.AddDays(-2),
                today.AddDays(-5)
            };

            var result = (int)methodInfo!.Invoke(controller, new object[] { activityDates })!;

            result.Should().Be(3);
        }

        [Fact]
        public void CalculateStreak_WhenMissedYesterday_ShouldReturnZero()
        {
            var controller = new UserController(null!);
            var methodInfo = typeof(UserController).GetMethod("CalculateStreak", BindingFlags.NonPublic | BindingFlags.Instance);
            var today = DateTime.UtcNow.Date;

            var activityDates = new List<DateTime>
            {
                today.AddDays(-2),
                today.AddDays(-3)
            };

            var result = (int)methodInfo!.Invoke(controller, new object[] { activityDates })!;

            result.Should().Be(0);
        }

        [Fact]
        public async Task GetUser_WhenUserExists_ShouldReturnOkWithUserData()
        {
            var options = new DbContextOptionsBuilder<LynqoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LynqoDbContext(options);

            var testUser = new User
            {
                Id = 1,
                Username = "testuser", 
                DisplayName = "Test Elek",
                Coins = 150,
                Email = "test@test.com",
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                PasswordHash = "dummyhash"
            };

            context.Users.Add(testUser);
            await context.SaveChangesAsync();

            var dbCount = await context.Users.CountAsync();
            dbCount.Should().Be(1, "mert az adatbázisnak tartalmaznia kell a mentett teszt usert");

            var controller = new UserController(context);

            var result = await controller.GetUser("testuser");

            var okResult = result.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>(
                "mert a felhasználó létezik, és 200 OK választ kellene kapnunk").Subject;

            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetUser_WhenUserDoesNotExist_ShouldReturnNotFound()
        {
            var options = new DbContextOptionsBuilder<LynqoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LynqoDbContext(options);
            var controller = new UserController(context);

            var result = await controller.GetUser("nemletezouser");

            var notFoundResult = result as NotFoundResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult!.StatusCode.Should().Be(404);
        }
    }
}