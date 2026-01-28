using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lynqo_Backend.Models
{
    [Table("media_files")]
    public class MediaFile
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("file_url")] // Matches your DB column
        public string FileUrl { get; set; }

        [Column("file_type")]
        public string FileType { get; set; }

        // Optional: If you want to use these, keep them. If not, they can stay null.
        [Column("uploader_id")]
        public int? UploaderId { get; set; }

        [Column("used_in")]
        public string? UsedIn { get; set; }

        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
