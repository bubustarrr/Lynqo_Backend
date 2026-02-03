// Report.cs
namespace LynqoBackend.Models
{
    public class Report
    {
        public int Id { get; set; }
        public int ReporterId { get; set; }
        public int? MessageId { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime CreatedAt { get; set; }
        public int? ResolvedBy { get; set; }

        public User Reporter { get; set; } = null!;
        public ChatMessage? Message { get; set; }
        public User? Resolver { get; set; }
    }
}
