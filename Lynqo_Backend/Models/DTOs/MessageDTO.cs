namespace LynqoBackend.Models.DTOs
{
    public class ChatMessageDTO
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class SendMessageDTO
    {
        public int ReceiverId { get; set; }
        public string Message { get; set; } = "";
    }

    public class ReportMessageDTO
    {
        public int MessageId { get; set; }
        public string Reason { get; set; } = "";
    }
}
