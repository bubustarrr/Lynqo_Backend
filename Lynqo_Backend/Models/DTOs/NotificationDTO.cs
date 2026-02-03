namespace LynqoBackend.Models.DTOs
{
    public class NotificationDTO
    {
        public int Id { get; set; }
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
