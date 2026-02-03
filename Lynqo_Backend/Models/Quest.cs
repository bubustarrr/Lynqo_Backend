// Quest.cs
namespace LynqoBackend.Models
{
    public class Quest
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int RewardXp { get; set; }
        public string Duration { get; set; } = "daily"; // daily | weekly
        public string Type { get; set; } = "lesson";    // lesson | practice
    }
}
