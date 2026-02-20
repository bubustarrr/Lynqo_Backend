using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lynqo_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Every request here requires a valid JWT token
    public class SettingsController : ControllerBase
    {
        private readonly LynqoDbContext _context;

        public SettingsController(LynqoDbContext context)
        {
            _context = context;
        }

        // GET: api/Settings
        // Retrieves the current logged-in user's settings
        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("Invalid user token.");
            }

            var settings = await _context.Settings.FirstOrDefaultAsync(s => s.UserId == userId);

            // If the user doesn't have settings yet, return default values
            if (settings == null)
            {
                return Ok(new UserSettingsDTO
                {
                    DarkMode = false,
                    SoundEnabled = true,
                    DailyGoalMinutes = 15,
                    UiLanguage = "en",
                    NotificationsEnabled = true
                });
            }

            // Return the user's actual settings mapped to the DTO
            return Ok(new UserSettingsDTO
            {
                DarkMode = settings.DarkMode,
                SoundEnabled = settings.SoundEnabled,
                DailyGoalMinutes = settings.DailyGoalMinutes,
                UiLanguage = settings.UiLanguage,
                NotificationsEnabled = settings.NotificationsEnabled
            });
        }

        // PUT: api/Settings
        // Updates or creates the user's settings
        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] UserSettingsDTO settingsDto)
        {
            if (settingsDto == null)
                return BadRequest("Invalid settings data.");

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("Invalid user token.");
            }

            // Try to find existing settings
            var existingSettings = await _context.Settings.FirstOrDefaultAsync(s => s.UserId == userId);

            if (existingSettings != null)
            {
                // Update existing record
                existingSettings.DarkMode = settingsDto.DarkMode;
                existingSettings.SoundEnabled = settingsDto.SoundEnabled;
                existingSettings.DailyGoalMinutes = settingsDto.DailyGoalMinutes;
                existingSettings.UiLanguage = settingsDto.UiLanguage;
                existingSettings.NotificationsEnabled = settingsDto.NotificationsEnabled;

                _context.Settings.Update(existingSettings);
            }
            else
            {
                // Create a new record if the user didn't have one
                var newSettings = new Setting
                {
                    UserId = userId,
                    DarkMode = settingsDto.DarkMode,
                    SoundEnabled = settingsDto.SoundEnabled,
                    DailyGoalMinutes = settingsDto.DailyGoalMinutes,
                    UiLanguage = settingsDto.UiLanguage,
                    NotificationsEnabled = settingsDto.NotificationsEnabled
                };

                _context.Settings.Add(newSettings);
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Settings updated successfully" });
        }
    }
}
