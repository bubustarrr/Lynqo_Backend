namespace Lynqo_Backend.Models
{
    public class Leaderboard
    {
        public int Id { get; set; }
        public string LeagueName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
