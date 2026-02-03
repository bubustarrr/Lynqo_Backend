// ChatMessage.cs
namespace LynqoBackend.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsReported { get; set; }

        public User Sender { get; set; } = null!;
        public User Receiver { get; set; } = null!;
    }
}
