namespace LynqoBackend.Models.DTOs
{
    public class QuestDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int RewardXp { get; set; }
        public string Duration { get; set; } = ""; // "daily" | "weekly"
        public string Type { get; set; } = "";     // "lesson" | "practice"
        public int Progress { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class QuestProgressRequest
    {
        public int QuestId { get; set; }
        public int ProgressDelta { get; set; } = 1;
    }

    public class QuestClaimRequest
    {
        public int QuestId { get; set; }
    }
}
