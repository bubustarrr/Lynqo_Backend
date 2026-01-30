using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("lessons")]
    public class Lesson
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("course_id")]
        public int CourseId { get; set; }

        // Added this based on our 'Units' discussion
        [Column("unit_id")]
        public int? UnitId { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("type")]
        public string Type { get; set; } = "mixed"; // mixed, listening, speaking, reading

        [Column("order_index")]
        public int OrderIndex { get; set; }

        [Column("xp_reward")]
        public int XpReward { get; set; } = 30;

        [Column("media_id")]
        public int? MediaId { get; set; } // Optional icon/cover image for the lesson

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property: Useful if you want to include the content automatically
        // e.g. _context.Lessons.Include(l => l.Contents)
        // You'll need a LessonContent model for this to work perfectly.
        // public List<LessonContent> Contents { get; set; } 
    }
}
