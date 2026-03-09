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
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly LynqoDbContext _context;

        public SettingsController(LynqoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("Invalid user token.");
            }

            var settings = await _context.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                return Ok(new UserSettingsDTO
                {
                    DarkMode = false,
                    SoundEnabled = true,
                    UiLanguage = "en",
                    NotificationsEnabled = true
                });
            }

            return Ok(new UserSettingsDTO
            {
                DarkMode = settings.DarkMode,
                SoundEnabled = settings.SoundEnabled,
                UiLanguage = settings.UiLanguage,
                NotificationsEnabled = settings.NotificationsEnabled
            });
        }

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

            var existingSettings = await _context.Settings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (existingSettings != null)
            {
                existingSettings.DarkMode = settingsDto.DarkMode;
                existingSettings.SoundEnabled = settingsDto.SoundEnabled;
                existingSettings.UiLanguage = settingsDto.UiLanguage;
                existingSettings.NotificationsEnabled = settingsDto.NotificationsEnabled;
            }
            else
            {
                var newSettings = new Setting
                {
                    UserId = userId,
                    DarkMode = settingsDto.DarkMode,
                    SoundEnabled = settingsDto.SoundEnabled,
                    UiLanguage = settingsDto.UiLanguage,
                    NotificationsEnabled = settingsDto.NotificationsEnabled
                };

                _context.Settings.Add(newSettings);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Settings updated successfully"
            });
        }
    }
}
