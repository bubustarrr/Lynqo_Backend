using System.Security.Claims;
using Lynqo_Backend.Data;
using Lynqo_Backend.Models;
using Lynqo_Backend.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly LynqoDbContext _context;
    private readonly AdminService _admin;
    public AdminController(LynqoDbContext context, AdminService admin) { _context = context; _admin = admin; }

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
