namespace LynqoBackend.Models.DTOs
{
    public class FriendDTO
    {
        public int FriendshipId { get; set; }  // The DB Row ID
        public int UserId { get; set; }        // The other user's ID
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
        public int RequestId { get; set; }     // Maps directly to FriendshipId
        public bool Accept { get; set; }       // True to accept, false to decline
    }
}
