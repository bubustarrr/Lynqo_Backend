using Lynqo_Backend.Data;
using Lynqo_Backend.Helpers;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
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
    public IActionResult Login([FromBody] UserLoginDTO dto)
    {
        var user = _context.Users.FirstOrDefault(u =>
            u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);

        if (user == null || !PasswordHasher.VerifyPassword(user.PasswordHash, dto.Password))
            return Unauthorized(new { error = "Invalid credentials." });

        var token = JwtHelper.GenerateJwtToken(user,
            _config["Jwt:Secret"],
            _config["Jwt:Issuer"],
            _config["Jwt:Audience"],
            _config.GetValue<int>("Jwt:ExpiresMinutes", 120));

        return Ok(new
        {
            token,
            user = new { user.Username, user.DisplayName, user.Email }
        });
    }
}
