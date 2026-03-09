using System.Security.Claims;
using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lynqo_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly LynqoDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(LynqoDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "Invalid token." });

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                var profileData = await BuildProfileDataAsync(user);
                return Ok(profileData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Crash in /me",
                    details = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetUserProfile(string username)
        {
            try
            {
                var normalizedUsername = username.Trim().ToLower();

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername);

                if (user == null)
                    return NotFound(new { message = $"User '{username}' not found." });

                var profileData = await BuildProfileDataAsync(user);
                return Ok(profileData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = $"Crash in /{username}",
                    details = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "Invalid token." });

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                if (!string.IsNullOrWhiteSpace(dto.Username))
                    user.Username = dto.Username.Trim();

                if (!string.IsNullOrWhiteSpace(dto.DisplayName))
                    user.DisplayName = dto.DisplayName.Trim();

                if (!string.IsNullOrWhiteSpace(dto.Email))
                    user.Email = dto.Email.Trim();

                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    // TODO: Replace with your real password hasher
                    // user.PasswordHash = YourPasswordHasher.Hash(dto.Password);
                }

                await _context.SaveChangesAsync();

                var profileData = await BuildProfileDataAsync(user);
                return Ok(profileData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Crash in PUT /me",
                    details = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpPost("me/avatar")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> UploadMyAvatar(IFormFile file)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "Invalid token." });

                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file uploaded." });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { message = "Only image files are allowed." });

                if (file.Length > 10_000_000)
                    return BadRequest(new { message = "File is too large." });

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                var webRoot = _environment.WebRootPath ??
                              Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                var uploadFolder = Path.Combine(
                    webRoot,
                    "media",
                    "images",
                    "profile_pictures"
                );

                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                if (!string.IsNullOrWhiteSpace(user.ProfilePicUrl) &&
                    user.ProfilePicUrl.StartsWith("/media/images/profile_pictures/"))
                {
                    try
                    {
                        var oldRelativePath = user.ProfilePicUrl
                            .TrimStart('/')
                            .Replace('/', Path.DirectorySeparatorChar);

                        var oldFullPath = Path.Combine(webRoot, oldRelativePath);

                        if (System.IO.File.Exists(oldFullPath))
                            System.IO.File.Delete(oldFullPath);
                    }
                    catch
                    {
                    }
                }

                var randomFileName = $"{Guid.NewGuid():N}{extension}";
                var fullFilePath = Path.Combine(uploadFolder, randomFileName);

                await using (var stream = new FileStream(fullFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var publicUrl = $"/media/images/profile_pictures/{randomFileName}";
                user.ProfilePicUrl = publicUrl;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Profile picture uploaded successfully.",
                    avatarUrl = publicUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Crash in /me/avatar",
                    details = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("id")?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim))
                return null;

            if (!int.TryParse(userIdClaim, out var userId))
                return null;

            return userId;
        }

        private async Task<object> BuildProfileDataAsync(User user)
        {
            int totalXp = 0;
            int currentStreak = 0;

            try
            {
                totalXp = await _context.UserXps
                    .Where(x => x.UserId == user.Id)
                    .Select(x => x.XpAmount)
                    .DefaultIfEmpty(0)
                    .SumAsync();
            }
            catch
            {
            }

            try
            {
                var completedDates = await _context.UserLessons
                    .Where(ul => ul.UserId == user.Id)
                    .Select(ul => ul.CompletedAt)
                    .ToListAsync();

                var dates = completedDates
                    .Select(d => d.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToList();

                if (dates.Any())
                {
                    var today = DateTime.UtcNow.Date;
                    var last = dates.First();

                    if (last >= today.AddDays(-1))
                    {
                        var expected = last;

                        foreach (var d in dates)
                        {
                            if (d == expected)
                            {
                                currentStreak++;
                                expected = expected.AddDays(-1);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return new
            {
                Username = user.Username,
                DisplayName = string.IsNullOrEmpty(user.DisplayName) ? user.Username : user.DisplayName,
                Email = user.Email,
                AvatarUrl = user.ProfilePicUrl,
                Hearts = user.Hearts,
                Coins = user.Coins,
                IsPremium = user.IsPremium,
                CreatedAt = user.CreatedAt.ToString("MMM yyyy"),
                TotalXp = totalXp,
                Streak = currentStreak
            };
        }
    }
}
