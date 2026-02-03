namespace Lynqo_Backend.Models.DTOs;

public class LessonAnalyticsCreateDTO
{
    public int? LessonId { get; set; }
    public int TimeSpentSeconds { get; set; }
    public decimal Accuracy { get; set; }
    public int Attempts { get; set; }
}

public class PracticeSessionCreateDTO
{
    public string Type { get; set; } = "vocabulary";
    public int Score { get; set; }
    public int XpEarned { get; set; }
    public int DurationSeconds { get; set; }
}
