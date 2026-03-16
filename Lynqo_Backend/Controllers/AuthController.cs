using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Linq;
using Lynqo_Backend.Data;
using Lynqo_Backend.Helpers;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.DTOs;
using Lynqo_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lynqo_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly LynqoDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthController(LynqoDbContext context, IConfiguration config, IEmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username || u.Email == dto.Email))
                return BadRequest(new { error = "Username or email is already taken." });

            var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

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
                CreatedAt = DateTime.UtcNow,
                IsVerified = false,
                VerificationToken = verificationToken
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var verifyUrl = $"{Request.Scheme}://{Request.Host}/api/auth/verify-email?token={verificationToken}";
            try
            {
                await _emailService.SendVerificationEmailAsync(user.Email, verifyUrl);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Registration successful, but an error occurred while sending the email.", details = ex.Message });
            }

            return Ok(new { message = "Registration successful! Please check your inbox and verify your email address." });
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest("Missing token.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);

            if (user == null)
                return BadRequest("Invalid or already used token.");

            user.IsVerified = true;
            user.VerificationToken = null;
            await _context.SaveChangesAsync();

            return Redirect("http://localhost:3000/verify-success");
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return BadRequest(new { error = "User not found." });
            if (user.IsVerified) return BadRequest(new { error = "Email is already verified." });

            user.VerificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            await _context.SaveChangesAsync();

            var verifyUrl = $"{Request.Scheme}://{Request.Host}/api/auth/verify-email?token={user.VerificationToken}";
            try
            {
                await _emailService.SendVerificationEmailAsync(user.Email, verifyUrl);
                return Ok(new { message = "Verification email resent successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to send email.", details = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return Ok(new { message = "If the email exists, a reset link has been sent." });

            user.ResetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            var resetUrl = $"http://localhost:3000/reset-password?token={user.ResetToken}";

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, resetUrl);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to send email.", details = ex.Message });
            }

            return Ok(new { message = "If the email exists, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == dto.Token);

            if (user == null || user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow)
                return BadRequest(new { error = "Invalid or expired token." });

            user.PasswordHash = PasswordHasher.HashPassword(dto.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password successfully reset." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);

            if (user == null || !PasswordHasher.VerifyPassword(user.PasswordHash, dto.Password))
                return Unauthorized(new { error = "Invalid credentials." });

            if (!user.IsVerified)
                return Unauthorized(new { error = "Please verify your email address first. Check your inbox!" });

            var activeBan = await _context.BannedUsers
                .FirstOrDefaultAsync(b => b.UserId == user.Id && (b.BannedUntil == null || b.BannedUntil > DateTime.UtcNow));

            if (activeBan != null)
                return Unauthorized(new { error = "Account banned.", reason = activeBan.Reason });

            var accessToken = JwtHelper.GenerateJwtToken(user, _config["Jwt:Key"], _config["Jwt:Issuer"], _config["Jwt:Audience"], 15);
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

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

            return Ok(new { token = accessToken, refreshToken = refreshToken, user = new { user.Username, user.DisplayName, user.Email, user.ProfilePicUrl } });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            return Ok();
        }
    }

    #region DTOs
    public class RefreshTokenRequest { public string RefreshToken { get; set; } = null!; }
    public class ResendVerificationDTO { public string Email { get; set; } = null!; }
    public class ForgotPasswordDTO { public string Email { get; set; } = null!; }
    public class ResetPasswordDTO { public string Token { get; set; } = null!; public string NewPassword { get; set; } = null!; }
    #endregion
}