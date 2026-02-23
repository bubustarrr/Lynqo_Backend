using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("user_xp")]
    public class UserXp
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("xp_amount")]
        public int XpAmount { get; set; }

        [Column("source")]
        public string Source { get; set; } = "lesson";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; } = null!;

    }
}
