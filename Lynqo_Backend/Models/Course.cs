using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("courses")]
    public class Course
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("source_language_id")]
        public int SourceLanguageId { get; set; } // e.g., 1 for English

        [Column("target_language_id")]
        public int TargetLanguageId { get; set; } // e.g., 2 for French

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // Navigation Properties (Optional, if you have a Language model)
        // public Language SourceLanguage { get; set; }
        // public Language TargetLanguage { get; set; }
    }
}
