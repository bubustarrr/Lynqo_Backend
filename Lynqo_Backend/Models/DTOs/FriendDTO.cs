namespace LynqoBackend.Models.DTOs
{

    public class FriendDTO
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Status { get; set; } = ""; // "pending" | "accepted" | "declined"
        public bool IsSender { get; set; }
    }

    public class FriendRequestDTO
    {
        public int TargetUserId { get; set; }
    }

    public class FriendRespondDTO
    {
        public int RequestId { get; set; }
        public bool Accept { get; set; }
    }
}
