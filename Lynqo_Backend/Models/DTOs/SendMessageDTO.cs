namespace LynqoBackend.Models.DTOs
{
    public class SendMessageDto
    {
        public int ReceiverId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
