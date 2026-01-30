using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("user_lessons")]
    public class UserLesson
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("lesson_id")]
        public int LessonId { get; set; }

        [Column("completed_at")]
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        [Column("stars")]
        public int Stars { get; set; } = 0; // e.g. 1, 2, 3 stars

        [Column("xp_earned")]
        public int XpEarned { get; set; } = 0;

        [Column("best_score")]
        public int BestScore { get; set; } = 0; // e.g. 100%
    }
}
