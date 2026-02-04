using Lynqo_Backend.Data;
using Lynqo_Backend.Helpers;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Lynqo_Backend.Controllers
{
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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO dto)
        {
            // (Keep your existing Register code exactly as is)
            if (_context.Users.Any(u => u.Username == dto.Username || u.Email == dto.Email))
                return BadRequest(new { error = "Username or email already taken." });

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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);

            if (user == null || !PasswordHasher.VerifyPassword(user.PasswordHash, dto.Password))
                return Unauthorized(new { error = "Invalid credentials." });

            Console.WriteLine($"[DEBUG] Checking ban for UserID: {user.Id}");

            var bans = await _context.BannedUsers.Where(b => b.UserId == user.Id).ToListAsync();
            Console.WriteLine($"[DEBUG] Found {bans.Count} ban records for this user.");

            foreach (var b in bans)
            {
                Console.WriteLine($"[DEBUG] Ban ID: {b.Id}, Expires: {b.BannedUntil}, IsActive: {b.BannedUntil == null || b.BannedUntil > DateTime.UtcNow}");
            }
            // ---------------------

            var activeBan = await _context.BannedUsers
                .Where(b => b.UserId == user.Id && (b.BannedUntil == null || b.BannedUntil > DateTime.UtcNow))
                .FirstOrDefaultAsync();

            if (activeBan != null)
            {
                Console.WriteLine("[DEBUG] User IS BANNED. Blocking."); // Log block
                return Unauthorized(new { error = "Account banned.", reason = activeBan.Reason });
            }
            else
            {
                Console.WriteLine("[DEBUG] User is NOT banned. Allowing."); // Log allow
            }

            // 1. Generate Access Token (JWT)
            var accessToken = JwtHelper.GenerateJwtToken(user,
                _config["Jwt:Key"], _config["Jwt:Issuer"], _config["Jwt:Audience"],
                15); // Short expiry (15 mins)

            // 2. Generate Refresh Token (Random String)
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            // 3. Save Refresh Token to DB
            var apiToken = new ApiToken
            {
                UserId = user.Id,
                Token = refreshToken,
                Scopes = "refresh_token",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30) // Long expiry (30 days)
            };
            _context.ApiTokens.Add(apiToken);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                token = accessToken,
                refreshToken = refreshToken, // Send this to frontend!
                user = new { user.Username, user.DisplayName, user.Email }
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            // 1. Find the token in DB
            var storedToken = await _context.ApiTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

            // 2. Validate it
            if (storedToken == null)
                return Unauthorized("Invalid refresh token.");

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _context.ApiTokens.Remove(storedToken); // Delete expired token
                await _context.SaveChangesAsync();
                return Unauthorized("Refresh token expired. Please login again.");
            }

            // 3. Generate NEW Access Token
            var newAccessToken = JwtHelper.GenerateJwtToken(storedToken.User,
                _config["Jwt:Key"], _config["Jwt:Issuer"], _config["Jwt:Audience"],
                15);

            // 4. Rotate Refresh Token (Optional security best practice: create a new one, delete old)
            var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            storedToken.Token = newRefreshToken; // Update existing row
            storedToken.ExpiresAt = DateTime.UtcNow.AddDays(30); // Extend life
            await _context.SaveChangesAsync();

            return Ok(new
            {
                token = newAccessToken,
                refreshToken = newRefreshToken
            });
        }
    }

    // DTO for the Refresh Request
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }
}
