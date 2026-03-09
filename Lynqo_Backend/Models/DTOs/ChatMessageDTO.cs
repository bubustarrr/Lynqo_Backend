using System;

namespace LynqoBackend.Models.DTOs
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsMine { get; set; }
    }
}
