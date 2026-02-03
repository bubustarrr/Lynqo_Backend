namespace Lynqo_Backend.Models.DTOs;

public class AiStartDTO { public int? LessonId { get; set; } }
public class AiSendMessageDTO { public string Message { get; set; } = ""; }
public class AiCompleteDTO { public int AiScore { get; set; } = 0; public string? AiFeedback { get; set; } }
