using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    public class UserBadge
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BadgeId { get; set; }
        [Column("earned_at")]
        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public Badge Badge { get; set; }
    }
}
