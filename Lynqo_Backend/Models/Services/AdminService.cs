using Lynqo_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Lynqo_Backend.Models.Services;

public class AdminService
{
    private readonly LynqoDbContext _context;
    public AdminService(LynqoDbContext context) => _context = context;

    public async Task<bool> IsAdminAsync(int userId)
        => await _context.Users.AnyAsync(u => u.Id == userId && u.Role == "admin");

    public async Task LogAdminAsync(int adminId, string actionType, int? targetUserId, string? description)
    {
        _context.AdminLogs.Add(new Models.AdminLog
        {
            AdminId = adminId,
            ActionType = actionType,
            TargetUserId = targetUserId,
            Description = description,
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }
}
