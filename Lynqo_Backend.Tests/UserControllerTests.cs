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
            // Arrange
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

            // Act
            var result = (int)methodInfo!.Invoke(controller, new object[] { activityDates })!;

            // Assert
            result.Should().Be(3);
        }

        [Fact]
        public void CalculateStreak_WhenMissedYesterday_ShouldReturnZero()
        {
            // Arrange
            var controller = new UserController(null!);
            var methodInfo = typeof(UserController).GetMethod("CalculateStreak", BindingFlags.NonPublic | BindingFlags.Instance);
            var today = DateTime.UtcNow.Date;

            var activityDates = new List<DateTime>
            {
                today.AddDays(-2),
                today.AddDays(-3)
            };

            // Act
            var result = (int)methodInfo!.Invoke(controller, new object[] { activityDates })!;

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task GetUser_WhenUserExists_ShouldReturnOkWithUserData()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<LynqoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LynqoDbContext(options);

            // 1. Létrehozzuk a teszt usert és figyelünk a pontos Username-re!
            var testUser = new User
            {
                Id = 1,
                Username = "testuser",  // <-- Ezt fogjuk keresni!
                DisplayName = "Test Elek",
                Coins = 150,
                Email = "test@test.com",
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                PasswordHash = "dummyhash"
            };

            context.Users.Add(testUser);
            await context.SaveChangesAsync();

            // 2. BIZTONSÁGI ELLENŐRZÉS: Tényleg benne van az adatbázisban?
            var dbCount = await context.Users.CountAsync();
            dbCount.Should().Be(1, "mert az adatbázisnak tartalmaznia kell a mentett teszt usert");

            var controller = new UserController(context);

            // Act: Lekérdezzük a "testuser" nevű usert
            var result = await controller.GetUser("testuser");

            // Assert: A BeOfType sokkal beszédesebb hibát ad, ha nem OK (200) a válasz!
            var okResult = result.Should().BeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>(
                "mert a felhasználó létezik, és 200 OK választ kellene kapnunk").Subject;

            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetUser_WhenUserDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<LynqoDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new LynqoDbContext(options);
            var controller = new UserController(context);

            // Act
            var result = await controller.GetUser("nemletezouser");

            // Assert
            var notFoundResult = result as NotFoundResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult!.StatusCode.Should().Be(404);
        }
    }
}