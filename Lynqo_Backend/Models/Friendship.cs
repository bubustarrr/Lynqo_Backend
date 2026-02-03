// Friendship.cs
namespace LynqoBackend.Models
{
    public class Friendship
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Status { get; set; } = "pending"; // pending | accepted | declined
        public DateTime CreatedAt { get; set; }

        public User Sender { get; set; } = null!;
        public User Receiver { get; set; } = null!;
    }
}
