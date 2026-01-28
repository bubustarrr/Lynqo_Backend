using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    public class UserXp
    {
        public int Id { get; set; }
        [Column("user_id")] // <--- FIX 1: Maps UserId to user_id
        public int UserId { get; set; }

        [Column("xp_amount")] // <--- FIX 2: Maps XpAmount to xp_amount
        public int XpAmount { get; set; }
        public string Source { get; set; } = "lesson"; // lesson, practice, legendary
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } // Navigation
    }
}
