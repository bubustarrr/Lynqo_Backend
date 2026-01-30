using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("units")]
    public class Unit
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("course_id")]
        public int CourseId { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("order_index")]
        public int OrderIndex { get; set; }

        // Navigation property (optional, handy for "Include" queries)
        public List<Lesson> Lessons { get; set; }
    }
}
