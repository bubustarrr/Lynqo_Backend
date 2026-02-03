// UserQuest.cs
namespace LynqoBackend.Models
{
    public class UserQuest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int QuestId { get; set; }
        public int Progress { get; set; }
        public DateTime? CompletedAt { get; set; }

        public User User { get; set; } = null!;
        public Quest Quest { get; set; } = null!;
    }
}
