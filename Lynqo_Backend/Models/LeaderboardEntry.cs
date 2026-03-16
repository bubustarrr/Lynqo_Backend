using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("leaderboard_entries")]
    public class LeaderboardEntry
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("leaderboard_id")]
        public int LeaderboardId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("xp")]
        public int Xp { get; set; }

        [Column("rank")]
        public int Rank { get; set; }

        public Leaderboard Leaderboard { get; set; }
        public User User { get; set; }
    }
}
