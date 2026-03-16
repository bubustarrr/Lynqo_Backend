using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Linq;
using Lynqo_Backend.Data;
using Lynqo_Backend.Helpers;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.DTOs;
using Lynqo_Backend.Services; // <-- ÚJ USING AZ EMAIL SERVICE-HEZ
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
        private readonly IEmailService _emailService; // <-- ÚJ MEZŐ

        // KONSTRUKTOR FRISSÍTVE
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

            // Generálunk egy egyedi tokent az email megerősítéshez
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
                IsVerified = false, // <-- ALAPÉRTELMEZETTEN FALSE
                VerificationToken = verificationToken // <-- ELMENTJÜK A TOKENT
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // KIKÜLDJÜK AZ EMAILT
            var verifyUrl = $"{Request.Scheme}://{Request.Host}/api/auth/verify-email?token={verificationToken}";
            try
            {
                await _emailService.SendVerificationEmailAsync(user.Email, verifyUrl);
            }
            catch (Exception ex)
            {
                // Ha beszakad az email küldés (pl. rossz jelszó a configban), a user azért létrejön, 
                // de ezt logolni kellene egy éles rendszerben.
                return StatusCode(500, new { error = "Regisztráció sikeres, de hiba történt az email kiküldésekor.", details = ex.Message });
            }

            return Ok(new { message = "Sikeres regisztráció! Kérlek erősítsd meg az email címedet a postaládádba kapott linkkel." });
        }

        // ÚJ VÉGPONT: Amikor a user rákattint az emailben a linkre!
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest("Hiányzó token.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);

            if (user == null)
                return BadRequest("Érvénytelen vagy már felhasznált token.");

            user.IsVerified = true;
            user.VerificationToken = null;
            await _context.SaveChangesAsync();

            // IDE IRÁNYÍTJUK ÁT A FRONTENDRE! (Írd át a portot, ha nem 5173)
            return Redirect("http://localhost:3000/verify-success");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);

            if (user == null || !PasswordHasher.VerifyPassword(user.PasswordHash, dto.Password))
                return Unauthorized(new { error = "Invalid credentials." });

            // --- ÚJ ELLENŐRZÉS: Megerősítette már az emailt? ---
            if (!user.IsVerified)
                return Unauthorized(new { error = "Please verify your email address first. Check your inbox!" });

            // Check if the user has an active ban
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

        // A Refresh végpont maradhat ugyanaz, ami volt (itt lerövidítettem, hogy ne foglalja a helyet)
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            /* Ide hagyd meg a régi Refresh metódusod tartalmát */
            return Ok(); // Ezt cseréld ki a te régi kódodra!
        }
    }

    #region DTOs
    public class RefreshTokenRequest { public string RefreshToken { get; set; } = null!; }
    #endregion
}