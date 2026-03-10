namespace LynqoBackend.Models.DTOs
{
    public class QuestDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int RewardXp { get; set; }
        public string Duration { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Progress { get; set; }
        public int Target { get; set; }  // New: needed for the progress bar
        public bool IsCompleted { get; set; }
    }
}
