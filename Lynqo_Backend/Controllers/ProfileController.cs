using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Lynqo_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly LynqoDbContext _context;

        public ProfileController(LynqoDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. GET: api/Profile/me  (My own profile)
        // ==========================================
        [HttpGet("me")]
        [Authorize] // Requires you to be logged in
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                // Get User ID from JWT Token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("id")?.Value
                               ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token." });
                }

                // Fetch User
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return NotFound(new { message = "User not found." });

                // Build Safe Response
                var profileData = await BuildProfileDataAsync(user);
                return Ok(profileData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Crash in /me", details = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // ==========================================
        // 2. GET: api/Profile/{username}  (Other profiles)
        // ==========================================
        [HttpGet("{username}")]
        public async Task<IActionResult> GetUserProfile(string username)
        {
            try
            {
                // Fetch User by username
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
                if (user == null) return NotFound(new { message = $"User '{username}' not found." });

                // Build Safe Response
                var profileData = await BuildProfileDataAsync(user);
                return Ok(profileData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Crash in /{username}", details = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // ==========================================
        // SAFE DATA BUILDER
        // ==========================================
        private async Task<object> BuildProfileDataAsync(User user)
        {
            int totalXp = 0;
            int currentStreak = 0;

            // 1. Safely fetch XP (If this table mapping is broken, XP just becomes 0)
            try
            {
                totalXp = await _context.UserXps.Where(x => x.UserId == user.Id).SumAsync(x => x.XpAmount);
            }
            catch { /* DB issue with UserXps, ignoring */ }

            // 2. Safely fetch Streak
            try
            {
                var dates = await _context.UserLessons
                    .Where(ul => ul.UserId == user.Id)
                    .Select(ul => ul.CompletedAt.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToListAsync();

                if (dates.Any())
                {
                    var today = DateTime.UtcNow.Date;
                    var last = dates.First();
                    if (last >= today.AddDays(-1))
                    {
                        DateTime expected = last;
                        foreach (var d in dates)
                        {
                            if (d == expected) { currentStreak++; expected = expected.AddDays(-1); }
                            else break;
                        }
                    }
                }
            }
            catch { /* DB issue with UserLessons, ignoring */ }

            // Return the cleanly mapped object!
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

            [HttpPut("me")]
            [Authorize]
            public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto dto)
            {
                // 1. Get user id from JWT
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("id")?.Value
                               ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized();

                // 2. Load user
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound();

                // 3. Update simple fields if provided
                if (!string.IsNullOrWhiteSpace(dto.Username))
                    user.Username = dto.Username.Trim();

                if (!string.IsNullOrWhiteSpace(dto.DisplayName))
                    user.DisplayName = dto.DisplayName.Trim();

                if (!string.IsNullOrWhiteSpace(dto.ProfilePicUrl))
                    user.ProfilePicUrl = dto.ProfilePicUrl.Trim();

                // 4. Update password if provided
                if (!string.IsNullOrEmpty(dto.Password))
                {
                    // Use the SAME hashing you use in your login/register code:
                    // user.PasswordHash = YourPasswordHasher.Hash(dto.Password);

                    // temporary unsafe version (only while testing, not for production):
                    // user.PasswordHash = dto.Password;
                }

                // 5. Save changes
                await _context.SaveChangesAsync();

                // 6. Option A: return nothing (204)
                // return NoContent();

                // 6. Option B: return updated profile in same shape as GET /me
                var profileData = await BuildProfileDataAsync(user);
                return Ok(profileData);
            }

        }
    }

