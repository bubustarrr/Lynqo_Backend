namespace Lynqo_Backend.Models;
public class PracticeSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = "vocabulary"; // vocabulary | listening | speaking
    public int Score { get; set; }
    public int XpEarned { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
}