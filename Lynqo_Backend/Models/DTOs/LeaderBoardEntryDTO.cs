namespace Lynqo_Backend.Models.DTOs
{
    public class LeaderboardEntryDTO
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public int Xp { get; set; }
        public int Rank { get; set; }
    }
}
