namespace Lynqo_Backend.Models;
public class AiMessage
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string Sender { get; set; } = "user"; // user | ai
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
    public AiSession Session { get; set; } = null!;
}