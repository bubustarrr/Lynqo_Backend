using System.Security.Claims;
using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lynqo_Backend.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly LynqoDbContext _context;
        private readonly AdminService _admin;

        public AdminController(LynqoDbContext context, AdminService admin)
        {
            _context = context;
            _admin = admin;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst("id") ?? User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (claim == null) throw new InvalidOperationException("User ID claim missing.");
            return int.Parse(claim.Value);
        }

        private async Task<IActionResult?> RequireAdmin()
        {
            var adminId = GetUserId();
            if (!await _admin.IsAdminAsync(adminId)) return Forbid();
            return null;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var forbid = await RequireAdmin();
            if (forbid != null) return forbid;

            var users = await _context.Users
                .Select(u => new
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    IsPremium = u.IsPremium,
                    IsBanned = _context.BannedUsers.Any(b => b.UserId == u.Id && (b.BannedUntil == null || b.BannedUntil > DateTime.UtcNow))
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPatch("users/{userId:int}/role")]
        public async Task<IActionResult> SetRole(int userId, [FromBody] RoleUpdateDto dto)
        {
            var forbid = await RequireAdmin();
            if (forbid != null) return forbid;
            var adminId = GetUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.Role = dto.Role;
            await _context.SaveChangesAsync();

            await _admin.LogAdminAsync(adminId, "change_role", userId, $"Role changed to: {dto.Role}");
            return Ok(new { message = "Szerepkör sikeresen frissítve." });
        }

        [HttpPatch("users/{userId:int}/profile-picture")]
        public async Task<IActionResult> SetProfilePic(int userId, [FromBody] ProfilePicUpdateDto dto)
        {
            var forbid = await RequireAdmin();
            if (forbid != null) return forbid;
            var adminId = GetUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.ProfilePicUrl = dto.ProfilePicUrl;
            await _context.SaveChangesAsync();

            await _admin.LogAdminAsync(adminId, "change_profile_pic", userId, "Admin modified profile picture.");
            return Ok(new { message = "Profilkép frissítve." });
        }

        [HttpPost("users/{userId:int}/subscription")]
        public async Task<IActionResult> GrantSubscription(int userId, [FromBody] SubscriptionCreateDto dto)
        {
            var forbid = await RequireAdmin();
            if (forbid != null) return forbid;
            var adminId = GetUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            DateTime startDate = DateTime.UtcNow;
            DateTime endDate = dto.DurationMonths == 12 ? startDate.AddYears(1) : startDate.AddMonths(1);

            var newSubscription = new LynqoBackend.Models.Subscription
            {
                UserId = userId,
                PlanName = dto.DurationMonths == 12 ? "Premium 1 Year" : "Premium 1 Month",
                QuantityMonths = dto.DurationMonths,
                StartsAt = startDate,
                ExpiresAt = endDate,
                AutoRenew = dto.AutoRenew, // <-- ITT BEÁLLÍTJUK AZ ÉRTÉKET A FRONTENDRÕL
                Provider = "Admin Panel",
                TransactionId = $"ADMIN-{Guid.NewGuid().ToString().Substring(0, 8)}"
            };

            _context.Subscriptions.Add(newSubscription);
            user.IsPremium = true;
            await _context.SaveChangesAsync();

            await _admin.LogAdminAsync(adminId, "grant_subscription", userId, $"Granted {dto.DurationMonths} month(s) premium. Auto-renew: {dto.AutoRenew}");
            return Ok(new { message = "Elõfizetés sikeresen hozzáadva." });
        }

        [HttpDelete("users/{userId:int}/subscription")]
        public async Task<IActionResult> RevokeSubscription(int userId)
        {
            var forbid = await RequireAdmin();
            if (forbid != null) return forbid;
            var adminId = GetUserId();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var activeSubs = await _context.Subscriptions
                .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var sub in activeSubs)
            {
                sub.ExpiresAt = DateTime.UtcNow;
                sub.AutoRenew = false;
            }

            user.IsPremium = false;
            await _context.SaveChangesAsync();

            await _admin.LogAdminAsync(adminId, "revoke_subscription", userId, "Premium subscription revoked.");
            return Ok(new { message = "Elõfizetés visszavonva." });
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports([FromQuery] string status = "pending")
        {
            var forbid = await RequireAdmin(); if (forbid != null) return forbid;

            var reports = await _context.Reports
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reports);
        }

        [HttpPost("reports/{id:int}/resolve")]
        public async Task<IActionResult> ResolveReport(int id)
        {
            var forbid = await RequireAdmin(); if (forbid != null) return forbid;
            var adminId = GetUserId();

            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = "resolved";
            report.ResolvedBy = adminId;
            await _context.SaveChangesAsync();

            await _admin.LogAdminAsync(adminId, "resolve_report", report.ReporterId, $"Report {id} resolved.");
            return Ok(new { message = "Resolved." });
        }

        [HttpPost("ban/{userId:int}")]
        public async Task<IActionResult> Ban(int userId, [FromQuery] string? reason = null, [FromQuery] DateTime? bannedUntil = null)
        {
            var forbid = await RequireAdmin(); if (forbid != null) return forbid;
            var adminId = GetUserId();

            _context.BannedUsers.Add(new BannedUser
            {
                UserId = userId,
                Reason = reason,
                BannedUntil = bannedUntil,
                IssuedBy = adminId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            await _admin.LogAdminAsync(adminId, "ban_user", userId, reason);
            return Ok(new { message = "User banned." });
        }

        [HttpDelete("ban/{userId:int}")]
        public async Task<IActionResult> Unban(int userId)
        {
            var forbid = await RequireAdmin(); if (forbid != null) return forbid;
            var adminId = GetUserId();

            var bans = await _context.BannedUsers.Where(b => b.UserId == userId).ToListAsync();
            _context.BannedUsers.RemoveRange(bans);
            await _context.SaveChangesAsync();

            await _admin.LogAdminAsync(adminId, "unban_user", userId, null);
            return Ok(new { message = "User unbanned." });
        }

        [HttpGet("logs/admin")]
        public async Task<IActionResult> GetAdminLogs([FromQuery] int take = 100)
        {
            var forbid = await RequireAdmin(); if (forbid != null) return forbid;

            var logs = await _context.AdminLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(Math.Clamp(take, 1, 500))
                .ToListAsync();

            return Ok(logs);
        }
    }

    public class RoleUpdateDto
    {
        public string Role { get; set; } = null!;
    }

    public class ProfilePicUpdateDto
    {
        public string ProfilePicUrl { get; set; } = null!;
    }

    public class SubscriptionCreateDto
    {
        public int DurationMonths { get; set; }
        public bool AutoRenew { get; set; } // <-- ÚJ MEZÕ ITT
    }
}