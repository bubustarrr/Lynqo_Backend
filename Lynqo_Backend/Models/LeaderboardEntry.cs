namespace Lynqo_Backend.Models
{
    public class LeaderboardEntry
    {
        public int Id { get; set; }
        public int LeaderboardId { get; set; }
        public int UserId { get; set; }
        public int Xp { get; set; }
        public int Rank { get; set; }

        public Leaderboard Leaderboard { get; set; }
        public User User { get; set; }
    }
}
