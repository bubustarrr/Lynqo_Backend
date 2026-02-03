namespace Lynqo_Backend.Models;
public class Analytics
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? LessonId { get; set; }
    public int TimeSpentSeconds { get; set; }
    public decimal Accuracy { get; set; }
    public int Attempts { get; set; }
    public DateTime? CompletedAt { get; set; }
    public User User { get; set; } = null!;
    public Lesson? Lesson { get; set; }
}