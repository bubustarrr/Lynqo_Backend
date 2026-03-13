using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Linq;
using Lynqo_Backend.Data;
using Lynqo_Backend.Helpers;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lynqo_Backend.Controllers
{
    /// <summary>
    /// Handles user authentication including registration, login, and token refreshing.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly LynqoDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(LynqoDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        /// <summary>
        /// Registers a new user with default stats (5 hearts, 0 coins, user role).
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username || u.Email == dto.Email))
                return BadRequest(new { error = "Username or email is already taken." });

            var user = new User
            {
                Username = dto.Username,
                DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.Username : dto.DisplayName,
                Email = dto.Email,
                PasswordHash = PasswordHasher.HashPassword(dto.Password),
                Hearts = 5,
                Coins = 0,
                IsPremium = false,
                Role = "user",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { user.Username, user.DisplayName, user.Email });
        }

        /// <summary>
        /// Authenticates a user, checks for active bans, and issues JWT and Refresh tokens.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);

            if (user == null || !PasswordHasher.VerifyPassword(user.PasswordHash, dto.Password))
                return Unauthorized(new { error = "Invalid credentials." });

            // Check if the user has an active ban
            var activeBan = await _context.BannedUsers
                .FirstOrDefaultAsync(b => b.UserId == user.Id && (b.BannedUntil == null || b.BannedUntil > DateTime.UtcNow));

            if (activeBan != null)
            {
                return Unauthorized(new { error = "Account banned.", reason = activeBan.Reason });
            }

            // 1. Generate Access Token (JWT) 
            var accessToken = JwtHelper.GenerateJwtToken(
                user,
                _config["Jwt:Key"],
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                15
            );

            // 2. Generate Refresh Token (Random 64-byte string)
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            // 3. Save Refresh Token to DB - 30 day expiry
            var apiToken = new ApiToken
            {
                UserId = user.Id,
                Token = refreshToken,
                Scopes = "refresh_token",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            _context.ApiTokens.Add(apiToken);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                token = accessToken,
                refreshToken = refreshToken,
                user = new { user.Username, user.DisplayName, user.Email }
            });
        }

        /// <summary>
        /// Generates a new JWT access token using a valid refresh token. Rotates the refresh token.
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            // 1. Find the token in the database
            var storedToken = await _context.ApiTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

            // 2. Validate token existence and expiration
            if (storedToken == null)
                return Unauthorized(new { error = "Invalid refresh token." });

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _context.ApiTokens.Remove(storedToken); // Clean up expired token
                await _context.SaveChangesAsync();
                return Unauthorized(new { error = "Refresh token expired. Please log in again." });
            }

            // 3. Generate NEW Access Token
            var newAccessToken = JwtHelper.GenerateJwtToken(
                storedToken.User,
                _config["Jwt:Key"],
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                15
            );

            // 4. Rotate Refresh Token (Security best practice: invalidate old, create new)
            var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            storedToken.Token = newRefreshToken;
            storedToken.ExpiresAt = DateTime.UtcNow.AddDays(30);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                token = newAccessToken,
                refreshToken = newRefreshToken
            });
        }
    }

    #region DTOs
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = null!;
    }
    #endregion
}
