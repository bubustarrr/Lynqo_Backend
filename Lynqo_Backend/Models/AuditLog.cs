public class AuditLog
{
    public int Id { get; set; }
    public string? EventType { get; set; }
    public int? UserId { get; set; }
    public string? Data { get; set; } // store JSON text
    public DateTime CreatedAt { get; set; }
    public User? User { get; set; }
}