using Lynqo_Backend.Models;

public class AiSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? LessonId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? AiFeedback { get; set; }
    public int AiScore { get; set; }
    public User User { get; set; } = null!;
    public Lesson? Lesson { get; set; }
    public ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
}