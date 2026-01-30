using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("lesson_contents")]
    public class LessonContent
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("lesson_id")]
        public int LessonId { get; set; }

        [Column("content_type")]
        public string ContentType { get; set; } = "text";
        // e.g., "text", "multiple_choice", "fill_blank", "speaking"

        [Column("question")]
        public string? Question { get; set; }

        [Column("answer")]
        public string? Answer { get; set; }

        [Column("options")]
        public string? Options { get; set; }
        // Stores JSON string: '["A", "B", "C"]' or '[{"text":"A", "audio":"..."}]'

        [Column("media_id")]
        public int? MediaId { get; set; } // Optional image/audio for the question

        // Optional: Helper property to parse JSON automatically (requires Newtonsoft or System.Text.Json)
        // [NotMapped]
        // public List<string> ParsedOptions => ...
    }
}
