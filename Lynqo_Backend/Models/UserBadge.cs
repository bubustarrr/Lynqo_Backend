using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("user_badges")] // EXACT MATCH: With underscore
    public class UserBadge
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")] // EXACT MATCH: With underscore
        public int UserId { get; set; }

        [Column("badge_id")] // EXACT MATCH: With underscore
        public int BadgeId { get; set; }

        [Column("earned_at")] // EXACT MATCH: With underscore
        public DateTime EarnedAt { get; set; }

        // Navigation property
        public Badge? Badge { get; set; }
    }
}
