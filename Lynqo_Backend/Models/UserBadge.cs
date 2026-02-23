using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("user_badges")]
    public class UserBadge
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("badge_id")]
        public int BadgeId { get; set; }

        [Column("earned_at")]
        public DateTime EarnedAt { get; set; }

        // Optional Navigation Properties
        public User? User { get; set; }
        public Badge? Badge { get; set; }
    }
}
