namespace Lynqo_Backend.Models;
public class AdminLog
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public string? ActionType { get; set; }
    public int? TargetUserId { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; }
    public User Admin { get; set; } = null!;
    public User? TargetUser { get; set; }
}